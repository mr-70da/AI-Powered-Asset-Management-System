using System.Text.Json;
using Kinana.AssetManagement.Application.Caching;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Kinana.AssetManagement.Infrastructure.Caching;

public sealed class RedisCacheService : ICacheService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ConnectionMultiplexer _connection;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheService(IOptions<CacheSettings> settings, ILogger<RedisCacheService> logger)
    {
        _logger = logger;

        var configuration = ConfigurationOptions.Parse(settings.Value.ConnectionString);
        configuration.AbortOnConnectFail = false;
        _connection = ConnectionMultiplexer.Connect(configuration);
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct)
    {
        try
        {
            var value = await _connection.GetDatabase().StringGetAsync(key);
            return value.IsNullOrEmpty
                ? default
                : JsonSerializer.Deserialize<T>((string)value!, JsonOptions);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis unavailable ({Key}) - serving from the database instead.", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct)
    {
        try
        {
            var json = JsonSerializer.Serialize(value, JsonOptions);
            await _connection.GetDatabase().StringSetAsync(key, json, ttl);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis unavailable ({Key}) - skipping cache write.", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken ct)
    {
        try
        {
            await _connection.GetDatabase().KeyDeleteAsync(key);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis unavailable ({Key}) - skipping cache delete.", key);
        }
    }

    public async Task RemoveByPrefixAsync(string pattern, CancellationToken ct)
    {
        try
        {
            var server = _connection.GetServer(_connection.GetEndPoints().First());
            var keys = server.Keys(pattern: pattern).ToArray();
            if (keys.Length == 0)
            {
                return;
            }

            await _connection.GetDatabase().KeyDeleteAsync(keys);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis unavailable ({Pattern}) - skipping prefix delete.", pattern);
        }
    }
}
