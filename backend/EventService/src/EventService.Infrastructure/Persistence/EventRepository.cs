using EventService.Domain.Abstractions;
using EventService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventService.Infrastructure.Persistence;

public class EventRepository : IEventRepository
{
    private readonly EventDbContext _db;

    public EventRepository(EventDbContext db) => _db = db;

    public async Task AddAsync(Event ev, CancellationToken ct = default) =>
        await _db.Events.AddAsync(ev, ct);

    public async Task<Event?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _db.Events.Include(e => e.Zones).FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<(IReadOnlyList<Event> Items, int Total)> SearchAsync(
        string? name, DateTime? fromDate, DateTime? toDate, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Events.Include(e => e.Zones).AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(name))
            query = query.Where(e => EF.Functions.ILike(e.Name, $"%{name}%"));
        if (fromDate.HasValue)
            query = query.Where(e => e.Date >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(e => e.Date <= toDate.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(e => e.Date)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
