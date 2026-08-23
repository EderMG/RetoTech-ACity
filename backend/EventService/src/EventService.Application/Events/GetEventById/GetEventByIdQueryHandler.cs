using EventService.Application.DTOs;
using EventService.Domain.Abstractions;
using MediatR;

namespace EventService.Application.Events.GetEventById;

public class GetEventByIdQueryHandler : IRequestHandler<GetEventByIdQuery, EventResponseDto?>
{
    private readonly IEventRepository _repository;

    public GetEventByIdQueryHandler(IEventRepository repository) => _repository = repository;

    public async Task<EventResponseDto?> Handle(GetEventByIdQuery request, CancellationToken cancellationToken)
    {
        var ev = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (ev is null) return null;

        return new EventResponseDto(
            ev.Id, ev.Name, ev.Date, ev.Venue, ev.Status.ToString(), ev.CreatedAt,
            ev.Zones.Select(z => new ZoneResponseDto(z.Id, z.Name, z.Price, z.Capacity)).ToList());
    }
}
