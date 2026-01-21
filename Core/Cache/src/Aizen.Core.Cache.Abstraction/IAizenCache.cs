using Aizen.Core.Cache.Abstraction.Common;

namespace Aizen.Core.Cache.Abstraction;

public interface IAizenCache
{
    Task<T> GetAsync<T>(CancellationToken token = default);

    Task<T> GetAsync<T>(string key, CancellationToken token = default);

    Task<(bool keyExists, T cacheItem)> TryGetAsync<T>(CancellationToken token = default);

    Task<(bool keyExists, T cacheItem)> TryGetAsync<T>(string key, CancellationToken token = default);

    Task SetAsync<T>(T cacheItem, AizenCacheOptions? cacheOptions = null, CancellationToken token = default);

    Task SetAsync<T>(T cacheItem, string key, AizenCacheOptions? cacheOptions = null, CancellationToken token = default);

    Task<bool> ExistsAsync<T>(CancellationToken token = default);

    Task<bool> ExistsAsync<T>(string key, CancellationToken token = default);

    Task RemoveAsync<T>(CancellationToken token = default);

    Task RemoveAsync<T>(string key, CancellationToken token = default);
    
}