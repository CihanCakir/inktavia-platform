using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Currency;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;
using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Core.CQRS.Abstraction.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Model;

namespace Aizen.Modules.ReferenceData.Application.Currency.Queries;

[DocumentationInfo("Returns details for a single currency by ID", "Cached for 24 hours; invalidated on currency changes.")]
public sealed class GetCurrencyDetailQueryHandler : AizenQueryHandler<GetCurrencyDetailQuery, CurrencyDto?>, IAizenQueryHandlerCacheable
{
    private readonly ICurrencyReferenceService _service;

    public GetCurrencyDetailQueryHandler(ICurrencyReferenceService service)
    {
        _service = service;
    }

    public override async Task<CurrencyDto?> Handle(GetCurrencyDetailQuery request, CancellationToken cancellationToken)
    {
        return await _service.GetByIdAsync(request.Id, cancellationToken);
    }

    public AizenCacheType CacheType => AizenCacheType.Distributed;
    public AizenCacheOptions CacheOptions => new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24) };
}
