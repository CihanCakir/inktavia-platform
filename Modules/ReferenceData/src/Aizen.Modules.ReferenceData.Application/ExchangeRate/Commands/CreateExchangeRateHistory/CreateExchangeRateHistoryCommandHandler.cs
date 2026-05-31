using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.ExchangeRate;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.ExchangeRate.Commands;

public sealed class CreateExchangeRateHistoryCommandHandler : AizenCommandHandler<CreateExchangeRateHistoryCommand, ExchangeRateHistoryDto>
{
    private readonly IExchangeRateReferenceService _service;

    public CreateExchangeRateHistoryCommandHandler(IExchangeRateReferenceService service)
    {
        _service = service;
    }

    public override async Task<ExchangeRateHistoryDto?> Handle(CreateExchangeRateHistoryCommand request, CancellationToken cancellationToken)
    {
        return await _service.CreateHistoryAsync(request.FromCurrencyCode, request.ToCurrencyCode, request.Rate, request.ProviderType, request.RateDate, request.RawProviderPayload, cancellationToken);
    }
}
