using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Currency;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;
using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Core.CQRS.Abstraction.Handler;

namespace Aizen.Modules.ReferenceData.Application.Currency.Queries;

[DocumentationInfo("Returns the configured base currency", "Cached for 24 hours; invalidated on currency changes.")]
public sealed class GetBaseCurrencyQueryHandler : AizenQueryHandler<GetBaseCurrencyQuery, CurrencyDto?>, IAizenQueryHandlerCacheable
{
    private readonly ICurrencyReferenceService _service;

    public GetBaseCurrencyQueryHandler(ICurrencyReferenceService service)
    {
        _service = service;
    }

    public override async Task<CurrencyDto?> Handle(GetBaseCurrencyQuery request, CancellationToken cancellationToken)
    {
        return await _service.GetBaseCurrencyAsync(cancellationToken);
    }

    public AizenCacheType CacheType => AizenCacheType.Distributed;
    public AizenCacheOptions CacheOptions => new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24) };
}
