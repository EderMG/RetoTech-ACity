using EventService.Application.DTOs;
using MediatR;

namespace EventService.Application.Events.GetEvents;

/// <summary>
/// Query (lado Read de CQRS). Soporta búsqueda avanzada por nombre/rango de fechas y paginación.
/// El handler consulta primero Redis (cache-aside) antes de ir a la BD.
/// </summary>
public record GetEventsQuery(
    string? Name,
    DateTime? FromDate,
    DateTime? ToDate,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<EventResponseDto>>;
