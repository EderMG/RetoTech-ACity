using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using NotificationService.Application.Consumers;
using NotificationService.Domain.Abstractions;
using NotificationService.Infrastructure.Email;
using NotificationService.Infrastructure.Persistence;

namespace NotificationService.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationServiceInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        // --- MongoDB (NoSQL) ---
        var mongoClient = new MongoClient(config.GetConnectionString("Mongo"));
        services.AddSingleton<IMongoDatabase>(_ => mongoClient.GetDatabase(config["Mongo:Database"] ?? "notificationservice"));
        services.AddScoped<INotificationJobRepository, MongoNotificationJobRepository>();

        // --- Email (MailKit) ---
        services.AddSingleton<IEmailSender>(_ => new MailKitEmailSender(
            config["Smtp:Host"] ?? "mailpit",
            int.Parse(config["Smtp:Port"] ?? "1025"),
            config["Smtp:From"] ?? "no-reply@eventos-platform.local"));

        // --- RabbitMQ / MassTransit con reintentos + DLQ ---
        services.AddMassTransit(bus =>
        {
            bus.AddConsumer<EventCreatedConsumer>();

            bus.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(config["RabbitMq:Host"] ?? "rabbitmq", "/", h =>
                {
                    h.Username(config["RabbitMq:User"] ?? "guest");
                    h.Password(config["RabbitMq:Password"] ?? "guest");
                });

                cfg.ReceiveEndpoint("notification-service-event-created", e =>
                {
                    // Reintentos con backoff exponencial (política simple requerida por el reto).
                    e.UseMessageRetry(r => r.Exponential(
                        retryLimit: 5,
                        minInterval: TimeSpan.FromSeconds(2),
                        maxInterval: TimeSpan.FromSeconds(30),
                        intervalDelta: TimeSpan.FromSeconds(2)));

                    // Tras agotar reintentos, MassTransit mueve automáticamente el mensaje
                    // a la cola "notification-service-event-created_error" (Dead Letter Queue).
                    e.ConfigureConsumer<EventCreatedConsumer>(context);
                });
            });
        });

        return services;
    }
}
