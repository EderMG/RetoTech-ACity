using System.Text;
using System.Threading.RateLimiting;
using EventService.Application.Events.CreateEvent;
using EventService.Application.Events.GetEventById;
using EventService.Application.Events.GetEvents;
using EventService.Infrastructure.DependencyInjection;
using EventService.Infrastructure.Persistence;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MediatR;

var builder = WebApplication.CreateBuilder(args);

// ---------- MediatR + Validation ----------
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateEventCommand).Assembly));
builder.Services.AddValidatorsFromAssembly(typeof(CreateEventCommand).Assembly);
builder.Services.AddFluentValidationAutoValidation();

// ---------- Infra (EF Core, Redis, MassTransit/RabbitMQ) ----------
builder.Services.AddEventServiceInfrastructure(builder.Configuration);

// ---------- JWT Authentication / Authorization por roles ----------
var jwtSection = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["SigningKey"]!))
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminOnly", p => p.RequireRole("Admin"))
    .AddPolicy("AnyUser", p => p.RequireRole("Admin", "User"));

// ---------- Rate limiting básico (anti-abuso) ----------
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

// ---------- Swagger / Health ----------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(builder.Configuration["Cors:AllowedOrigin"] ?? "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

// ---------- Migraciones automáticas en Development (demo local) ----------
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<EventDbContext>();
    db.Database.Migrate();

    app.UseSwagger();
    app.UseSwaggerUI();
}

// ---------- Manejo de errores seguro (sin exponer stack traces) ----------
app.UseExceptionHandler(errApp =>
{
    errApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        // Nunca se filtran detalles de la excepción ni de la BD al cliente.
        await context.Response.WriteAsJsonAsync(new { error = "Ocurrió un error interno. Intente nuevamente más tarde." });
    });
});

app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");

// ---------- Endpoints ----------
// El rate limiting global (por IP) ya se aplica a través de UseRateLimiter() más abajo.
var events = app.MapGroup("/events");

// POST /events -> requiere rol Admin
events.MapPost("/", async (CreateEventCommand command, IMediator mediator) =>
{
    var result = await mediator.Send(command);
    return Results.Created($"/events/{result.Id}", result);
})
.RequireAuthorization("AdminOnly")
.WithName("CreateEvent")
.Produces<EventService.Application.DTOs.EventResponseDto>(StatusCodes.Status201Created);

// GET /events -> accesible a Admin y User, con cache Redis
events.MapGet("/", async (string? name, DateTime? fromDate, DateTime? toDate, int page, int pageSize, IMediator mediator) =>
{
    var query = new GetEventsQuery(name, fromDate, toDate, page <= 0 ? 1 : page, pageSize <= 0 ? 20 : pageSize);
    var result = await mediator.Send(query);
    return Results.Ok(result);
})
.RequireAuthorization("AnyUser")
.WithName("SearchEvents");

// GET /events/{id} -> detalle
events.MapGet("/{id:guid}", async (Guid id, IMediator mediator) =>
{
    var result = await mediator.Send(new GetEventByIdQuery(id));
    return result is null ? Results.NotFound() : Results.Ok(result);
})
.RequireAuthorization("AnyUser")
.WithName("GetEventById");

app.Run();
