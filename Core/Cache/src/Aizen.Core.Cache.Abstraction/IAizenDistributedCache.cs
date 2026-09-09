using Aizen.Core.Cache.Abstraction.Common;
using System.Text.Json;

namespace Aizen.Core.Cache.Abstraction;

public interface IAizenDistributedCache : IAizenCache
{
    Task<T> GetNoHash<T>(string key, CancellationToken token = default);
    Task<dynamic> GetDirectStringToDynamic(string key, CancellationToken token = default);

    Task<bool> RemoveNoHash(string key, CancellationToken token = default);

    /// <summary>
    /// Evicts an entry written by the query read-cache decorator (Microsoft
    /// <c>RedisCache</c>), applying the SAME <c>InstanceName</c> prefix the read/set path
    /// uses. Use this — NOT <see cref="RemoveNoHash"/> — to invalidate cached query results:
    /// <c>RemoveNoHash</c> targets the raw key without the instance prefix and would miss the
    /// physical key the read path stored (e.g. <c>"Vessel:{HandlerName}:{hash}"</c>).
    /// </summary>
    Task<bool> RemoveReadCacheEntry(string key, CancellationToken token = default);

    Task<bool> ExistNoHash(string key, CancellationToken token = default);
    Task<bool> SetNoHash<T>(string key, T value, TimeSpan ttl, CancellationToken token = default);
    Task<AizenStringCacheItem<T>> GetNoHashWitTtl<T>(string key, CancellationToken token = default);
}