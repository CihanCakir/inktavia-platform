using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.ExchangeRate;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.ExchangeRate.Queries;

public sealed class GetExchangeRateQueryHandler : AizenQueryHandler<GetExchangeRateQuery, ExchangeRateDto?>
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
}
