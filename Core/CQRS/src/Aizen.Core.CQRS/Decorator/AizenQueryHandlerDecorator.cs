using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
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

        if (!this._isCacheable)
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

    private string GetCacheKey(TQuery request)
    {
        var cacheKey = CreateCacheKey(request);
        return $"{this._decorated.GetType().Name}:{cacheKey}";
    }

    private static string CreateCacheKey(object obj, string? propName = null)
    {
        var sb = new StringBuilder();
        if (obj.GetType().IsValueType || obj is string)
        {
            _ = sb.AppendFormat(CultureInfo.CurrentCulture, "{0}_{1}|", propName, obj);
        }
        else if (obj.GetType().GetProperties().Count(x => x.Name != "QueryId") == 0)
        {
            return "";
        }
        else
        {
            foreach (var prop in obj.GetType().GetProperties())
            {
                if (prop.Name == "QueryId")
                {
                    continue;
                }

                if (typeof(IEnumerable<object>).IsAssignableFrom(prop.PropertyType))
                {
                    var get = prop.GetGetMethod()!;
                    if (!get.IsStatic && get.GetParameters().Length == 0)
                    {
                        var collection = (IEnumerable<object>)get.Invoke(obj, null)!;
                        foreach (var o in collection)
                        {
                            _ = sb.Append(CreateCacheKey(o, prop.Name));
                        }
                    }
                }
                else
                {
                    _ = sb.AppendFormat(CultureInfo.CurrentCulture, "{0}{1}_{2}|", propName, prop.Name,
                        prop.GetValue(obj, null));
                }
            }
        }

        return AizenHash.ComputeHash(AizenHashType.Sha256, sb.ToString());
    }
}