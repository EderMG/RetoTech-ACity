namespace EventService.Domain.Abstractions;

/// <summary>
/// Puerto de dominio (patrón hexagonal) para publicar eventos de integración.
/// La implementación concreta (MassTransit + RabbitMQ) vive en Infrastructure.
/// </summary>
public interface IEventPublisher
{
    Task PublishEventCreatedAsync(Entities.Event ev, Guid correlationId, CancellationToken ct = default);
}
