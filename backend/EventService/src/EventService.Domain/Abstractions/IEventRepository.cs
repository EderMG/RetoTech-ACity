using EventService.Domain.Entities;

namespace EventService.Domain.Abstractions;

public interface IEventRepository
{
    Task AddAsync(Event ev, CancellationToken ct = default);
    Task<Event?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<(IReadOnlyList<Event> Items, int Total)> SearchAsync(
        string? name, DateTime? fromDate, DateTime? toDate, int page, int pageSize, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
