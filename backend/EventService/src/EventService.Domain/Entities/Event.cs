using EventService.Domain.Enums;

namespace EventService.Domain.Entities;

/// <summary>
/// Aggregate root del contexto "Eventos". Encapsula sus zonas y garantiza invariantes
/// (no se puede crear un evento sin zonas, fecha futura, etc.)
/// </summary>
public class Event
{
    private readonly List<Zone> _zones = new();

    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!;
    public DateTime Date { get; private set; }
    public string Venue { get; private set; } = default!;
    public EventStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public IReadOnlyCollection<Zone> Zones => _zones.AsReadOnly();

    private Event() { } // EF Core

    public Event(string name, DateTime date, string venue, IEnumerable<Zone> zones)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre del evento es obligatorio.", nameof(name));
        if (date <= DateTime.UtcNow)
            throw new ArgumentException("La fecha del evento debe ser futura.", nameof(date));
        if (string.IsNullOrWhiteSpace(venue))
            throw new ArgumentException("El lugar es obligatorio.", nameof(venue));

        var zoneList = zones?.ToList() ?? new List<Zone>();
        if (zoneList.Count == 0)
            throw new ArgumentException("El evento debe tener al menos una zona.", nameof(zones));

        Id = Guid.NewGuid();
        Name = name;
        Date = date;
        Venue = venue;
        Status = EventStatus.Draft;
        CreatedAt = DateTime.UtcNow;

        foreach (var zone in zoneList)
        {
            zone.AssignToEvent(Id);
            _zones.Add(zone);
        }
    }

    public void Publish()
    {
        if (Status != EventStatus.Draft)
            throw new InvalidOperationException("Solo un evento en borrador puede publicarse.");
        Status = EventStatus.Published;
    }
}
