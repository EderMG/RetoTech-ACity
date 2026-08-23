using EventService.Application.DTOs;
using MediatR;

namespace EventService.Application.Events.CreateEvent;

/// <summary>
/// Comando (CQRS - MediatR) que representa la intención de crear + publicar un evento.
/// Maneja la creación transaccional en BD y la publicación asíncrona del mensaje EventCreated.
/// </summary>
public record CreateEventCommand(
    string Name,
    DateTime Date,
    string Venue,
    List<ZoneDto> Zones) : IRequest<EventResponseDto>;
