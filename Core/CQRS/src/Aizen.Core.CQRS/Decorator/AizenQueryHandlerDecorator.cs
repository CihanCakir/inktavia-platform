using System.Diagnostics.CodeAnalysis;
using Aizen.Core.Cache.Abstraction;
using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Core.CQRS.Abstraction.Handler;
using Aizen.Core.CQRS.Abstraction.Message;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Security;
using Microsoft.Extensions.DependencyInjection;

namespace Aizen.Core.CQRS.Decorator;

[SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global")]
internal sealed class AizenQueryHandlerDecorator<TQuery, TResult> : AizenQueryHandler<TQuery, TResult>,
    IAizenRequestDecorator<TQuery, TResult>
    where TQuery : IAizenQuery<TResult>
    where TResult : class
{
    public IAizenRequestHandler<TQuery, TResult> Decorated => _decorated;
    
    private readonly IAizenQueryHandler<TQuery, TResult> _decorated;

    private readonly AizenCacheOptions? _cacheOptions;

    private readonly IAizenCache? _cache;

    private readonly bool _isCacheable;

    public AizenQueryHandlerDecorator(
        IAizenQueryHandler<TQuery, TResult> decorated,
        IServiceProvider serviceProvider)
    {
        _decorated = decorated;
        this._isCacheable = decorated is IAizenQueryHandlerCacheable;

        if (this._isCacheable)
        {
            this._cacheOptions = ((IAizenQueryHandlerCacheable)decorated).CacheOptions;
            this._cache = ((IAizenQueryHandlerCacheable)decorated).CacheType switch
            {
                AizenCacheType.Memory => serviceProvider.GetService<IAizenMemoryCache>(),
                AizenCacheType.Distributed => serviceProvider.GetService<IAizenDistributedCache>(),
                AizenCacheType.Mixed => serviceProvider.GetService<IAizenCache>(),
                _ => serviceProvider.GetService<IAizenCache>(),
            };
        }
    }

    public override async Task<TResult> Handle(TQuery request, CancellationToken cancellationToken)
    {
        TResult result;

        // Bypass the cache when the handler is not cacheable, or when a cacheable handler opts THIS request out
        // (e.g. responses embedding time-limited presigned URLs must always be fresh).
        if (!this._isCacheable
            || !((IAizenQueryHandlerCacheable)this._decorated).ShouldCache(request!))
        {
            result = await this._decorated.Handle(request, cancellationToken);
            return result;
        }

        var cacheKey = this.GetCacheKey(request);
        var (keyExists, cacheItem) = await this._cache!.TryGetAsync<TResult>(cacheKey, cancellationToken);

        if (keyExists)
        {
            result = cacheItem;
        }
        else
        {
            result = await this._decorated.Handle(request, cancellationToken);
            await this._cache.SetAsync(result, cacheKey, this._cacheOptions, cancellationToken);
        }

        return result;
    }

    // Canonical read-cache key. Built through AizenQueryCacheKey so that module
    // cache-invalidation services can reproduce the exact same key on the write path.
    private string GetCacheKey(TQuery request)
        => AizenQueryCacheKey.ForQuery(this._decorated.GetType().Name, request);
}