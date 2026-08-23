namespace EventService.Application.Common;

/// <summary>Puerto de dominio para caché (implementado con Redis en Infrastructure).</summary>
public interface ICacheService
{
    Task<string?> GetAsync(string key, CancellationToken ct = default);
    Task SetAsync(string key, string value, TimeSpan ttl, CancellationToken ct = default);
    Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default);
}
