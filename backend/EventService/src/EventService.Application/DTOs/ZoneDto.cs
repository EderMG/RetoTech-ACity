namespace EventService.Application.DTOs;

public record ZoneDto(string Name, decimal Price, int Capacity);

public record ZoneResponseDto(Guid Id, string Name, decimal Price, int Capacity);
