using EventService.Application.DTOs;
using MediatR;

namespace EventService.Application.Events.GetEventById;

public record GetEventByIdQuery(Guid Id) : IRequest<EventResponseDto?>;
