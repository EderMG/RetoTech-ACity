using NotificationService.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddNotificationServiceInfrastructure(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Manejo de errores seguro: no se filtran stack traces al cliente.
app.UseExceptionHandler(errApp =>
{
    errApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { error = "Ocurrió un error interno." });
    });
});

// NotificationService es principalmente un consumidor asíncrono (MassTransit hosted service).
// Se expone /health para observabilidad mínima (liveness/readiness probes).
app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Ok(new { service = "NotificationService", status = "running" }));

app.Run();
