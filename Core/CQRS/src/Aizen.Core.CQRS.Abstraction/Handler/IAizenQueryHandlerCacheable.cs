using Aizen.Core.Cache.Abstraction.Common;

namespace Aizen.Core.CQRS.Abstraction.Handler;

public interface IAizenQueryHandlerCacheable
{
    public AizenCacheType CacheType { get; }

    public AizenCacheOptions CacheOptions { get; }
}