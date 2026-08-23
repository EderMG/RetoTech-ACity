using EventService.Domain.Abstractions;
using MassTransit;
using Shared.Contracts;

namespace EventService.Infrastructure.Messaging;

/// <summary>
/// Adaptador que traduce el evento de dominio a un mensaje de integración versionado
/// y lo publica en RabbitMQ vía MassTransit. MassTransit ya provee reintentos y
/// serialización; la idempotencia del lado consumidor se implementa en NotificationService.
/// </summary>
public class MassTransitEventPublisher : IEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public MassTransitEventPublisher(IPublishEndpoint publishEndpoint) => _publishEndpoint = publishEndpoint;

    public async Task PublishEventCreatedAsync(Domain.Entities.Event ev, Guid correlationId, CancellationToken ct = default)
    {
        var message = new EventCreatedMessage
        {
            MessageId = Guid.NewGuid(),
            EventId = ev.Id,
            Name = ev.Name,
            OccurredAt = DateTime.UtcNow,
            CorrelationId = correlationId,
            Version = 1,
            EventDate = ev.Date,
            Venue = ev.Venue,
            PayloadHash = ComputeHash(ev)
        };

        await _publishEndpoint.Publish(message, ct);
    }

    private static string ComputeHash(Domain.Entities.Event ev)
    {
        var raw = $"{ev.Id}|{ev.Name}|{ev.Date:O}|{ev.Venue}";
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes);
    }
}
