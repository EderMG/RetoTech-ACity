using MassTransit;
using Microsoft.Extensions.Logging;
using NotificationService.Domain.Abstractions;
using NotificationService.Domain.Entities;
using Shared.Contracts;

namespace NotificationService.Application.Consumers;

/// <summary>
/// Consumidor del mensaje "EventCreated". Responsabilidades del reto:
///  - Idempotencia: si el MessageId ya fue procesado, se descarta silenciosamente.
///  - Persistencia de un registro de auditoría/notificación por cada mensaje.
///  - Envío de correo (best-effort; un fallo de correo no debe re-encolar infinitamente).
///  - Reintentos: gestionados en dos niveles -> MassTransit (a nivel de transporte, ver DI)
///    y aquí a nivel de negocio (RetryCount) antes de marcar DeadLettered.
/// La política de reintentos + moveTo _error (DLQ) se configura en el DI de Infrastructure.
/// </summary>
public class EventCreatedConsumer : IConsumer<EventCreatedMessage>
{
    private readonly INotificationJobRepository _repository;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<EventCreatedConsumer> _logger;

    public EventCreatedConsumer(
        INotificationJobRepository repository,
        IEmailSender emailSender,
        ILogger<EventCreatedConsumer> logger)
    {
        _repository = repository;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<EventCreatedMessage> context)
    {
        var message = context.Message;

        // --- Idempotencia: evita procesar el mismo MessageId dos veces ---
        if (await _repository.ExistsByMessageIdAsync(message.MessageId, context.CancellationToken))
        {
            _logger.LogInformation("MessageId {MessageId} ya procesado. Ignorando duplicado.", message.MessageId);
            return;
        }

        var job = new NotificationJob
        {
            MessageId = message.MessageId,
            EventId = message.EventId,
            EventName = message.Name,
            OccurredAt = message.OccurredAt,
            CorrelationId = message.CorrelationId,
            PayloadHash = message.PayloadHash,
            Status = NotificationStatus.Received
        };

        await _repository.AddAsync(job, context.CancellationToken);

        try
        {
            // Correo de demo a una casilla fija; en producción vendría del organizador/suscriptores.
            await _emailSender.SendEventCreatedEmailAsync(
                toAddress: "notificaciones-demo@eventos-platform.local",
                eventName: message.Name,
                eventDate: message.EventDate,
                venue: message.Venue,
                ct: context.CancellationToken);

            job.Status = NotificationStatus.Processed;
            job.ProcessedAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            job.RetryCount++;
            job.LastError = ex.Message;
            job.Status = NotificationStatus.Failed;
            _logger.LogWarning(ex, "Fallo al enviar correo para EventId {EventId}. Reintento {RetryCount}.", message.EventId, job.RetryCount);
            await _repository.UpdateAsync(job, context.CancellationToken);

            // Relanzamos para que MassTransit aplique la política de reintentos configurada;
            // tras agotar los reintentos, el bus mueve el mensaje a la cola _error (DLQ).
            throw;
        }

        await _repository.UpdateAsync(job, context.CancellationToken);
    }
}
