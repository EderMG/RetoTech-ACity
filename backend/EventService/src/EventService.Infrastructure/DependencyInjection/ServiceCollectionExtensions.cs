using EventService.Application.Common;
using EventService.Domain.Abstractions;
using EventService.Infrastructure.Caching;
using EventService.Infrastructure.Messaging;
using EventService.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace EventService.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEventServiceInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        // --- PostgreSQL / EF Core ---
        services.AddDbContext<EventDbContext>(options =>
            options.UseNpgsql(config.GetConnectionString("Postgres")));

        services.AddScoped<IEventRepository, EventRepository>();

        // --- Redis (cache-aside) ---
        var redisConnection = config.GetConnectionString("Redis") ?? "redis:6379";
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnection));
        services.AddScoped<ICacheService, RedisCacheService>();

        // --- RabbitMQ / MassTransit ---
        services.AddMassTransit(bus =>
        {
            bus.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(config["RabbitMq:Host"] ?? "rabbitmq", "/", h =>
                {
                    h.Username(config["RabbitMq:User"] ?? "guest");
                    h.Password(config["RabbitMq:Password"] ?? "guest");
                });

                // Requerimiento: reintentos ante fallos de publicación/consumo.
                cfg.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
                cfg.ConfigureEndpoints(context);
            });
        });

        services.AddScoped<IEventPublisher, MassTransitEventPublisher>();

        return services;
    }
}
