using System.Text.Json;
using EventService.Application.Common;
using EventService.Application.DTOs;
using EventService.Domain.Abstractions;
using MediatR;

namespace EventService.Application.Events.GetEvents;

public class GetEventsQueryHandler : IRequestHandler<GetEventsQuery, PagedResult<EventResponseDto>>
{
    private readonly IEventRepository _repository;
    private readonly ICacheService _cache;

    public GetEventsQueryHandler(IEventRepository repository, ICacheService cache)
    {
        _repository = repository;
        _cache = cache;
    }

    public async Task<PagedResult<EventResponseDto>> Handle(GetEventsQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"events:list:{request.Name}:{request.FromDate:yyyyMMdd}:{request.ToDate:yyyyMMdd}:{request.Page}:{request.PageSize}";

        var cached = await _cache.GetAsync(cacheKey, cancellationToken);
        if (cached is not null)
            return JsonSerializer.Deserialize<PagedResult<EventResponseDto>>(cached)!;

        var (items, total) = await _repository.SearchAsync(
            request.Name, request.FromDate, request.ToDate, request.Page, request.PageSize, cancellationToken);

        var dtoItems = items.Select(ev => new EventResponseDto(
            ev.Id, ev.Name, ev.Date, ev.Venue, ev.Status.ToString(), ev.CreatedAt,
            ev.Zones.Select(z => new ZoneResponseDto(z.Id, z.Name, z.Price, z.Capacity)).ToList())).ToList();

        var result = new PagedResult<EventResponseDto>(dtoItems, total, request.Page, request.PageSize);

        // TTL corto: la búsqueda de eventos es de lectura frecuente pero tolera datos de hasta ~30s.
        await _cache.SetAsync(cacheKey, JsonSerializer.Serialize(result), TimeSpan.FromSeconds(30), cancellationToken);

        return result;
    }
}
