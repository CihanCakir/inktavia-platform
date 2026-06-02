using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.ExchangeRate;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.ExchangeRate.Queries;

public sealed class GetExchangeRateHistoryQueryHandler : AizenQueryHandler<GetExchangeRateHistoryQuery, IReadOnlyList<ExchangeRateHistoryDto>>
{
    private readonly IExchangeRateReferenceService _service;

    public GetExchangeRateHistoryQueryHandler(IExchangeRateReferenceService service)
    {
        _service = service;
    }

    public override async Task<IReadOnlyList<ExchangeRateHistoryDto>> Handle(GetExchangeRateHistoryQuery request, CancellationToken cancellationToken)
    {
        return await _service.GetHistoryAsync(request.FromCurrencyCode, request.ToCurrencyCode, request.StartDate, request.EndDate, cancellationToken);
    }
}
