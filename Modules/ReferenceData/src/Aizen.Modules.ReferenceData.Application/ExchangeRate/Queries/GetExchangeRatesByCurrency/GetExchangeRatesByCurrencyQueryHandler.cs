using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.ExchangeRate;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.ExchangeRate.Queries;

public sealed class GetExchangeRatesByCurrencyQueryHandler : AizenQueryHandler<GetExchangeRatesByCurrencyQuery, IReadOnlyList<ExchangeRateDto>>
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
}
