namespace NotificationService.Domain.Entities;

public enum NotificationStatus
{
    Received = 0,
    Processed = 1,
    Failed = 2,
    DeadLettered = 3
}

/// <summary>
/// Registro persistente de cada mensaje EventCreated procesado. La clave `MessageId`
/// (única) es la base de la idempotencia del consumidor: si ya existe, no se reprocesa.
/// </summary>
public class NotificationJob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MessageId { get; set; }
    public Guid EventId { get; set; } = default!;
    public string EventName { get; set; } = default!;
    public DateTime OccurredAt { get; set; }
    public Guid CorrelationId { get; set; }
    public string PayloadHash { get; set; } = default!;
    public NotificationStatus Status { get; set; } = NotificationStatus.Received;
    public int RetryCount { get; set; }
    public string? LastError { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
}
