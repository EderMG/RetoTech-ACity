using EventService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EventService.Api;

/// <summary>
/// Permite ejecutar `dotnet ef migrations add ...` sin levantar toda la app.
/// Usa una cadena de conexión local; en runtime el DbContext real se configura vía DI (appsettings).
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<EventDbContext>
{
    public EventDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<EventDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=eventservice;Username=eventservice;Password=devpassword");
        return new EventDbContext(optionsBuilder.Options);
    }
}
