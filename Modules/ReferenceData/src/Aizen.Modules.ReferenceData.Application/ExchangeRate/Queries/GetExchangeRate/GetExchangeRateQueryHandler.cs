using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.ExchangeRate;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;
using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Core.CQRS.Abstraction.Handler;

namespace Aizen.Modules.ReferenceData.Application.ExchangeRate.Queries;

[DocumentationInfo("Returns the current exchange rate between two currencies", "Cached for 15 minutes; invalidated on rate updates.")]
public sealed class GetExchangeRateQueryHandler : AizenQueryHandler<GetExchangeRateQuery, ExchangeRateDto?>, IAizenQueryHandlerCacheable
{
    private readonly IExchangeRateReferenceService _service;

    public GetExchangeRateQueryHandler(IExchangeRateReferenceService service)
    {
        _service = service;
    }

    public override async Task<ExchangeRateDto?> Handle(GetExchangeRateQuery request, CancellationToken cancellationToken)
    {
        return await _service.GetCurrentRateAsync(request.FromCurrencyCode, request.ToCurrencyCode, cancellationToken);
    }

    public AizenCacheType CacheType => AizenCacheType.Distributed;
    public AizenCacheOptions CacheOptions => new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15) };
}
