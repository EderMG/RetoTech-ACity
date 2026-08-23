namespace EventService.Application.DTOs;

public record EventResponseDto(
    Guid Id,
    string Name,
    DateTime Date,
    string Venue,
    string Status,
    DateTime CreatedAt,
    IReadOnlyCollection<ZoneResponseDto> Zones);

public record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);
