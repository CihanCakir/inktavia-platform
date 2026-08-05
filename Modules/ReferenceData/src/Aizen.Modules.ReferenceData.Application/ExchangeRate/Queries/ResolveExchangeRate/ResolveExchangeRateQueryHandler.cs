using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Core.CQRS.Abstraction.Handler;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.ExchangeRate;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.ExchangeRate.Queries;

[DocumentationInfo(
    "Resolves the effective exchange rate between two currencies at a point in time (R1).",
    "Cached for 15 minutes keyed by from/to/as-of-day; invalidated on rate updates (current UTC day).")]
public sealed class ResolveExchangeRateQueryHandler
    : AizenQueryHandler<ResolveExchangeRateQuery, ExchangeRateResolveDto>, IAizenQueryHandlerCacheable
{
    private readonly IExchangeRateReferenceService _service;

    public ResolveExchangeRateQueryHandler(IExchangeRateReferenceService service)
    {
        _service = service;
    }

    public override Task<ExchangeRateResolveDto> Handle(ResolveExchangeRateQuery request, CancellationToken cancellationToken)
    {
        return _service.ResolveRateAsync(request.FromCurrencyCode, request.ToCurrencyCode, request.AsOfUtc, cancellationToken);
    }

    public AizenCacheType CacheType => AizenCacheType.Distributed;
    public AizenCacheOptions CacheOptions => new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15) };
}
