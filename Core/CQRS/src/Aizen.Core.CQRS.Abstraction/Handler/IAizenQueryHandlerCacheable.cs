using Aizen.Core.Cache.Abstraction.Common;

namespace Aizen.Core.CQRS.Abstraction.Handler;

public interface IAizenQueryHandlerCacheable
{
    public AizenCacheType CacheType { get; }

    public AizenCacheOptions CacheOptions { get; }

    /// <summary>
    /// Per-request cache opt-out. Return <c>false</c> to bypass the read cache for THIS request even though the
    /// handler is otherwise cacheable — e.g. a response that embeds time-limited presigned URLs must always be served
    /// fresh, never from a cache entry that can outlive the URL. Defaults to <c>true</c> (cache every request).
    /// </summary>
    bool ShouldCache(object request) => true;
}