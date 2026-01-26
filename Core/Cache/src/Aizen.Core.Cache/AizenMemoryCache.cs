using Aizen.Core.Cache.Abstraction;
using Aizen.Core.Cache.Abstraction.Common;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Aizen.Core.Cache;

internal sealed class AizenMemoryCache : AizenCacheBase, IAizenMemoryCache, IMemoryCache
{
    private readonly IMemoryCache _memoryCache;

    public AizenMemoryCache(IOptions<MemoryCacheOptions> options)
    {
        this._memoryCache = new MemoryCache(options);
    }

    public override Task<T> GetAsync<T>(string key, CancellationToken token = default)
    {
        return Task.Run(() =>
        {
            var cacheItem = this._memoryCache.Get<AizenCacheItem<T>>(key);

            if (cacheItem == null)
            {
                throw new AizenCacheNotFoundException(key);
            }

            return cacheItem.Value;
        }, token);
    }

    public override Task<(bool keyExists, T cacheItem)> TryGetAsync<T>(string key,
        CancellationToken token = default) where T : default
    {
        return Task.Run(() =>
        {
            var cacheItem = this._memoryCache.Get<AizenCacheItem<T>>(key);
            return (cacheItem != null, cacheItem != null ? cacheItem.Value : default(T));
        }, token)!;
    }

    public override Task SetAsync<T>(T cacheItem, string key, AizenCacheOptions? cacheOptions = null,
        CancellationToken token = default)
    {
        var memoryCacheEntryOptions = new MemoryCacheEntryOptions()
        {
            AbsoluteExpiration = DateTimeOffset.Now.AddHours(3),
            SlidingExpiration = TimeSpan.FromHours(1)
        };

        if (cacheOptions != null)
        {
            memoryCacheEntryOptions.AbsoluteExpiration = cacheOptions.AbsoluteExpiration;
            memoryCacheEntryOptions.AbsoluteExpirationRelativeToNow = cacheOptions.AbsoluteExpirationRelativeToNow;
            memoryCacheEntryOptions.SlidingExpiration = cacheOptions.SlidingExpiration;
        }

        return Task.Run(
            () => { _ = this._memoryCache.Set(key, new AizenCacheItem<T>(cacheItem), memoryCacheEntryOptions); }, token);
    }

    public override Task<bool> ExistsAsync<T>(string key, CancellationToken token = default)
    {
        return Task.Run(() =>
        {
            var cacheItem = this._memoryCache.Get<AizenCacheItem<T>>(key);
            return cacheItem != null;
        }, token);
    }

    public override Task RemoveAsync<T>(string key, CancellationToken token = default)
    {
        return Task.Run(() => { this._memoryCache.Remove(key); }, token);
    }

    #region IMemoryCache implementation

    public void Dispose()
    {
        this._memoryCache.Dispose();
    }

    public bool TryGetValue(object key, out object value)
    {
        return this._memoryCache.TryGetValue(key, out value);
    }

    public ICacheEntry CreateEntry(object key)
    {
        return this._memoryCache.CreateEntry(key);
    }

    public void Remove(object key)
    {
        this._memoryCache.Remove(key);
    }

    #endregion IMemoryCache implementation
}