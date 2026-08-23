namespace Shared.Contracts;

/// <summary>
/// Contrato de integración publicado por EventService en RabbitMQ (exchange "event-service")
/// y consumido por NotificationService. Versionado explícito para evolución del esquema.
/// </summary>
public sealed record EventCreatedMessage
{
    public Guid MessageId { get; init; } = Guid.NewGuid();
    public Guid EventId { get; init; }
    public string Name { get; init; } = default!;
    public DateTime OccurredAt { get; init; }
    public Guid CorrelationId { get; init; }
    public int Version { get; init; } = 1;

    // Campos adicionales útiles para el correo / auditoría (no rompen compatibilidad, opcionales)
    public DateTime EventDate { get; init; }
    public string Venue { get; init; } = default!;
    public string PayloadHash { get; init; } = default!;
}
