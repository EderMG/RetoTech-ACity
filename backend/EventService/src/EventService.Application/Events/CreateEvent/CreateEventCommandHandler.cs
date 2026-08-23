using EventService.Application.DTOs;
using EventService.Domain.Abstractions;
using EventService.Domain.Entities;
using MediatR;

namespace EventService.Application.Events.CreateEvent;

/// <summary>
/// Orquesta el caso de uso "Crear Evento":
///  1) Construye el aggregate (invariantes en el dominio).
///  2) Persiste en una única transacción (evento + zonas) vía IEventRepository/UnitOfWork.
///  3) Publica el mensaje de integración "EventCreated" (asíncrono, best-effort con outbox opcional).
/// </summary>
public class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, EventResponseDto>
{
    private readonly IEventRepository _repository;
    private readonly IEventPublisher _publisher;

    public CreateEventCommandHandler(IEventRepository repository, IEventPublisher publisher)
    {
        _repository = repository;
        _publisher = publisher;
    }

    public async Task<EventResponseDto> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        var zones = request.Zones.Select(z => new Zone(z.Name, z.Price, z.Capacity));
        var ev = new Event(request.Name, request.Date, request.Venue, zones);

        await _repository.AddAsync(ev, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken); // transacción: Event + Zones

        var correlationId = Guid.NewGuid();
        // Publicación asíncrona post-commit. En producción: patrón Outbox para garantizar
        // atomicidad entre el commit de BD y el envío del mensaje.
        await _publisher.PublishEventCreatedAsync(ev, correlationId, cancellationToken);

        return new EventResponseDto(
            ev.Id, ev.Name, ev.Date, ev.Venue, ev.Status.ToString(), ev.CreatedAt,
            ev.Zones.Select(z => new ZoneResponseDto(z.Id, z.Name, z.Price, z.Capacity)).ToList());
    }
}
