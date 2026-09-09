using Aizen.Core.Cache.Abstraction;
using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Core.Cache.Extension;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;
using System.Text.Json;
using Elastic.Apm.StackExchange.Redis;

namespace Aizen.Core.Cache;

internal sealed class AizenDistributedCache : AizenCacheBase, IAizenDistributedCache, IDistributedCache
{
    private readonly IDistributedCache _distributedCache;
    // The InstanceName that RedisCache prepends to every read/set key. The raw StackExchange
    // path (_database) does NOT prepend it, so read-cache eviction must add it explicitly.
    private readonly string _instanceName;
    private static ConnectionMultiplexer _connectionMultiplexer;
    private static IDatabase _database;
    public AizenDistributedCache(IOptions<RedisCacheOptions> options)
    {
        this._distributedCache = new RedisCache(options);
        this._instanceName = options.Value.InstanceName ?? string.Empty;
        _connectionMultiplexer = ConnectionMultiplexer.Connect(Connection.RedisConnection);
        _connectionMultiplexer.UseElasticApm();
        _database = _connectionMultiplexer.GetDatabase();

    }

    public override async Task<T> GetAsync<T>(
        string key,
        CancellationToken token = default)
    {
        var data = await this._distributedCache.GetStringAsync(key, token);


        if (string.IsNullOrEmpty(data))
        {
            throw new AizenCacheNotFoundException(key);
        }

        return JObject.Parse(data).ToObject<T>();
    }
    public async Task<JsonElement> GetDirectStringToJsonElement(string key, CancellationToken token = default)
    {
        JsonElement response;
        var data = await this._distributedCache.GetStringAsync(key, token);

        if (string.IsNullOrEmpty(data))
        {
            throw new AizenCacheNotFoundException(key);
        }
        using (JsonDocument outerDocument = JsonDocument.Parse(data))
        {
            JsonElement root = outerDocument.RootElement;
            string innerJsonValue = root.GetProperty("Value").GetString();
            using (JsonDocument innerDocument = JsonDocument.Parse(innerJsonValue))
            {
                JsonElement innerRoot = innerDocument.RootElement;
                response = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(innerRoot);
            }
        }
        return response;

    }

    public async Task<dynamic> GetDirectStringToDynamic(string key, CancellationToken token = default)
    {
        dynamic response;
        var data = await this._distributedCache.GetStringAsync(key, token);

        if (string.IsNullOrEmpty(data))
        {
            throw new AizenCacheNotFoundException(key);
        }
        using (JsonDocument outerDocument = JsonDocument.Parse(data))
        {
            JsonElement root = outerDocument.RootElement;
            string innerJsonValue = root.GetProperty("Value").GetString();
            response = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(innerJsonValue);
        }
        return response;

    }


    public IDatabase Database => _database;

    public override async Task<(bool keyExists, T cacheItem)> TryGetAsync<T>(string key,
        CancellationToken token = default) where T : default
    {
        var data = await this._distributedCache.GetStringAsync(key, token);

        return (string.IsNullOrEmpty(data)
            ? (false, default(T))
            : (true, JObject.Parse(data).ToObject<AizenCacheItem<T>>()!.Value))!;
    }

    public override Task SetAsync<T>(T cacheItem, string key, AizenCacheOptions? cacheOptions = null,
        CancellationToken token = default)
    {
        var distributedCacheEntryOptions = new DistributedCacheEntryOptions()
        {
            AbsoluteExpiration = DateTimeOffset.Now.AddDays(1),
            SlidingExpiration = TimeSpan.FromHours(1)
        };

        if (cacheOptions != null)
        {
            distributedCacheEntryOptions.AbsoluteExpiration = cacheOptions.AbsoluteExpiration;
            distributedCacheEntryOptions.AbsoluteExpirationRelativeToNow = cacheOptions.AbsoluteExpirationRelativeToNow;
            distributedCacheEntryOptions.SlidingExpiration = cacheOptions.SlidingExpiration;
        }

        var data = JObject.FromObject(new AizenCacheItem<T>(cacheItem)).ToString();

        return this._distributedCache.SetStringAsync(key, data, distributedCacheEntryOptions, token);
    }

    public override async Task<bool> ExistsAsync<T>(string key, CancellationToken token = default)
    {
        var data = await this._distributedCache.GetStringAsync(key, token);
        return !string.IsNullOrEmpty(data);
    }

    public override Task RemoveAsync<T>(string key, CancellationToken token = default)
    {
        // Await the removal (previously fire-and-forget: the discarded Task swallowed
        // failures and could race the caller's next read).
        return this._distributedCache.RemoveAsync(key, token);
    }

    #region IDistributedCache implementation

    public byte[] Get(string key)
    {
        return this._distributedCache.Get(key);
    }

    public Task<byte[]> GetAsync(string key, CancellationToken token = default)
    {
        return this._distributedCache.GetAsync(key, token);
    }

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        this._distributedCache.Set(key, value, options);
    }

    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options,
        CancellationToken token = default)
    {
        return this._distributedCache.SetAsync(key, value, options, token);
    }

    public void Refresh(string key)
    {
        this._distributedCache.Refresh(key);
    }

    public Task RefreshAsync(string key, CancellationToken token = default)
    {
        return this._distributedCache.RefreshAsync(key, token);
    }

    public void Remove(string key)
    {
        this._distributedCache.Remove(key);
    }

    public Task RemoveAsync(string key, CancellationToken token = default)
    {
        return this._distributedCache.RemoveAsync(key, token);
    }

    public async Task<T> GetNoHash<T>(string key, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();

        // Single async round trip: a null/missing key returns default without a
        // separate KeyExists probe (the old sync Any() blocked a thread-pool worker).
        var jsonData = await Database.StringGetAsync(key);
        if (jsonData.IsNullOrEmpty)
            return default;

        return JsonConvert.DeserializeObject<T>(jsonData!);
    }

    public async Task<bool> AnyAsync(string key, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        return await Database.KeyExistsAsync(key);
    }

    public static void SetDatabase(int db)
    {
        _database = _connectionMultiplexer.GetDatabase(db);
    }


    public async Task<bool> RemoveNoHash(string key, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        // KeyDeleteAsync already reports whether the key existed — no separate KeyExists round trip.
        return await Database.KeyDeleteAsync(key, CommandFlags.None);
    }

    public async Task<bool> RemoveReadCacheEntry(string key, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        // The read/set path (RedisCache) stores under InstanceName + key on the same Redis DB,
        // so evict the InstanceName-prefixed physical key.
        var physicalKey = _instanceName + key;
        return await Database.KeyDeleteAsync(physicalKey, CommandFlags.None);
    }

    public async Task<bool> ExistNoHash(string key, CancellationToken token = default)
    {
        return await AnyAsync(key, token);
    }

    public async Task<bool> SetNoHash<T>(string key, T value, TimeSpan ttl, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        var jsonString = JsonConvert.SerializeObject(value);

        return await Database.StringSetAsync(key, jsonString, ttl);
    }

    public async Task<AizenStringCacheItem<T>> GetNoHashWitTtl<T>(string key, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();

        // Single async round trip: fetch value + TTL together; a missing key returns
        // default without a preceding sync KeyExists probe.
        var redisData = await Database.StringGetWithExpiryAsync(key);
        if (redisData.Value.IsNullOrEmpty || redisData.Expiry is null)
            return default;

        var data = JsonConvert.DeserializeObject<T>(redisData.Value!);
        var exp = Convert.ToInt32(redisData.Expiry.Value.TotalSeconds);
        return new AizenStringCacheItem<T>(data, exp, redisData.Expiry.Value);
    }



    #endregion IDistributedCache implementation
}