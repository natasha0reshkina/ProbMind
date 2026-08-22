using System.Text.Json;
using ProbMind.Application.Abstractions.Cache;
using StackExchange.Redis;

namespace ProbMind.Infrastructure.Cache;

public sealed class RedisApplicationCache : IApplicationCache
{
    private readonly IDatabase _database;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public RedisApplicationCache(IConnectionMultiplexer multiplexer)
    {
        _database = multiplexer.GetDatabase();
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var value = await _database.StringGetAsync(key);
        if (value.IsNullOrEmpty)
            return default;

        return JsonSerializer.Deserialize<T>(value.ToString(), _json);
    }

    public Task SetAsync<T>(
        string key,
        T value,
        TimeSpan ttl,
        CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(value, _json);
        return _database.StringSetAsync(key, json, ttl);
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default) =>
        _database.KeyDeleteAsync(key);
}
