using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.ExchangeRate;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;
using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Core.CQRS.Abstraction.Handler;

namespace Aizen.Modules.ReferenceData.Application.ExchangeRate.Queries;

[DocumentationInfo("Returns all exchange rates for a given currency", "Cached for 15 minutes; invalidated on rate updates.")]
public sealed class GetExchangeRatesByCurrencyQueryHandler : AizenQueryHandler<GetExchangeRatesByCurrencyQuery, IReadOnlyList<ExchangeRateDto>>, IAizenQueryHandlerCacheable
{
    private readonly IExchangeRateReferenceService _service;

    public GetExchangeRatesByCurrencyQueryHandler(IExchangeRateReferenceService service)
    {
        _service = service;
    }

    public override async Task<IReadOnlyList<ExchangeRateDto>> Handle(GetExchangeRatesByCurrencyQuery request, CancellationToken cancellationToken)
    {
        return await _service.GetRatesByCurrencyAsync(request.CurrencyCode, cancellationToken);
    }

    public AizenCacheType CacheType => AizenCacheType.Distributed;
    public AizenCacheOptions CacheOptions => new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15) };
}
