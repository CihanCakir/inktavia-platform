using Aizen.Core.Cache.Abstraction.Common;
using System.Text.Json;

namespace Aizen.Core.Cache.Abstraction;

public interface IAizenDistributedCache : IAizenCache
{
    Task<T> GetNoHash<T>(string key);
    Task<dynamic> GetDirectStringToDynamic(string key, CancellationToken token = default);

    Task<bool> RemoveNoHash(string key);

    /// <summary>
    /// Evicts an entry written by the query read-cache decorator (Microsoft
    /// <c>RedisCache</c>), applying the SAME <c>InstanceName</c> prefix the read/set path
    /// uses. Use this — NOT <see cref="RemoveNoHash"/> — to invalidate cached query results:
    /// <c>RemoveNoHash</c> targets the raw key without the instance prefix and would miss the
    /// physical key the read path stored (e.g. <c>"Vessel:{HandlerName}:{hash}"</c>).
    /// </summary>
    Task<bool> RemoveReadCacheEntry(string key);

    Task<bool> ExistNoHash(string key);
    Task<bool> SetNoHash<T>(string key, T value, TimeSpan ttl);
    Task<AizenStringCacheItem<T>> GetNoHashWitTtl<T>(string key);
}