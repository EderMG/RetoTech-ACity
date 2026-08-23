using EventService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventService.Infrastructure.Persistence;

/// <summary>
/// DbContext de EventService. Usa PostgreSQL. El esquema se gestiona con Migraciones EF
/// (ver README para el comando `dotnet ef database update`).
/// </summary>
public class EventDbContext : DbContext
{
    public EventDbContext(DbContextOptions<EventDbContext> options) : base(options) { }

    public DbSet<Event> Events => Set<Event>();
    public DbSet<Zone> Zones => Set<Zone>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EventDbContext).Assembly);
    }
}
