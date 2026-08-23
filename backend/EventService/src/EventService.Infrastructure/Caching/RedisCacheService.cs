using EventService.Application.Common;
using StackExchange.Redis;

namespace EventService.Infrastructure.Caching;

/// <summary>
/// Implementación de ICacheService sobre Redis (cache-aside). Requerimiento del reto:
/// "Listar Eventos ... Incluir almacenamiento en cache con Redis".
/// </summary>
public class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;

    public RedisCacheService(IConnectionMultiplexer redis) => _redis = redis;

    private IDatabase Db => _redis.GetDatabase();

    public async Task<string?> GetAsync(string key, CancellationToken ct = default)
    {
        var value = await Db.StringGetAsync(key);
        return value.HasValue ? value.ToString() : null;
    }

    public async Task SetAsync(string key, string value, TimeSpan ttl, CancellationToken ct = default) =>
        await Db.StringSetAsync(key, value, ttl);

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        var endpoints = _redis.GetEndPoints();
        foreach (var endpoint in endpoints)
        {
            var server = _redis.GetServer(endpoint);
            await foreach (var key in server.KeysAsync(pattern: $"{prefix}*"))
                await Db.KeyDeleteAsync(key);
        }
    }
}
