namespace EventService.Domain.Entities;

/// <summary>
/// Zona de un evento (ej. VIP, General). Entidad hija dentro del agregado Event.
/// </summary>
public class Zone
{
    public Guid Id { get; private set; }
    public Guid EventId { get; private set; }
    public string Name { get; private set; } = default!;
    public decimal Price { get; private set; }
    public int Capacity { get; private set; }

    private Zone() { } // EF Core

    public Zone(string name, decimal price, int capacity)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre de la zona es obligatorio.", nameof(name));
        if (price < 0)
            throw new ArgumentException("El precio no puede ser negativo.", nameof(price));
        if (capacity <= 0)
            throw new ArgumentException("El aforo debe ser mayor a 0.", nameof(capacity));

        Id = Guid.NewGuid();
        Name = name;
        Price = price;
        Capacity = capacity;
    }

    internal void AssignToEvent(Guid eventId) => EventId = eventId;
}
