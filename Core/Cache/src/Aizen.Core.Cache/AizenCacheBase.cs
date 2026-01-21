using Aizen.Core.Cache.Abstraction;
using Aizen.Core.Cache.Abstraction.Common;

namespace Aizen.Core.Cache;

internal abstract class AizenCacheBase : IAizenCache
{
    public Task<T> GetAsync<T>(CancellationToken token = default)
    {
        var result = this.GetAsync<T>(typeof(T).FullName!, token);

        return result;
    }

    public abstract Task<T> GetAsync<T>(string key, CancellationToken token = default);

    public Task<(bool keyExists, T cacheItem)> TryGetAsync<T>(
        CancellationToken token = default)
    {
        var result = this.TryGetAsync<T>(typeof(T).FullName!, token);

        return result;
    }

    public abstract Task<(bool keyExists, T cacheItem)> TryGetAsync<T>(string key,
        CancellationToken token = default);

    public Task SetAsync<T>(T cacheItem, AizenCacheOptions? cacheOptions = null, CancellationToken token = default)
    {
        var result = this.SetAsync(cacheItem, typeof(T).FullName!, cacheOptions, token);

        return result;
    }

    public abstract Task SetAsync<T>(
        T cacheItem,
        string key,
        AizenCacheOptions? cacheOptions = null,
        CancellationToken token = default);

    public Task<bool> ExistsAsync<T>(CancellationToken token = default)
    {
        return this.ExistsAsync<T>(typeof(T).FullName!, token);
    }

    public abstract Task<bool> ExistsAsync<T>(string key, CancellationToken token = default);

    public Task RemoveAsync<T>(CancellationToken token = default)
    {
        var fullName = typeof(T).FullName;
        if (fullName != null)
        {
            var result = this.RemoveAsync<T>(fullName, token);

            return result;
        }
        
        throw new ArgumentNullException(nameof(fullName));
    }

    public abstract Task RemoveAsync<T>(string key, CancellationToken token = default);
}