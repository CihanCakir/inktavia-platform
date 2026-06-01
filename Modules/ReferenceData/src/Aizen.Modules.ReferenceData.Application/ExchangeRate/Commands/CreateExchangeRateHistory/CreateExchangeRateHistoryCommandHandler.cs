using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.ExchangeRate;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.ExchangeRate.Commands;

public sealed class CreateExchangeRateHistoryCommandHandler : AizenCommandHandler<CreateExchangeRateHistoryCommand, ExchangeRateHistoryDto>
{
    private readonly IExchangeRateReferenceService _service;
    private readonly IReferenceDataCacheInvalidationService _invalidation;

    public CreateExchangeRateHistoryCommandHandler(IExchangeRateReferenceService service, IReferenceDataCacheInvalidationService invalidation)
    {
        _service = service;
        _invalidation = invalidation;
    }

    public override async Task<ExchangeRateHistoryDto?> Handle(CreateExchangeRateHistoryCommand request, CancellationToken cancellationToken)
    {
        var result = await _service.CreateHistoryAsync(request.FromCurrencyCode, request.ToCurrencyCode, request.Rate, request.ProviderType, request.RateDate, request.RawProviderPayload, cancellationToken);
        await _invalidation.InvalidateExchangeRateAsync(request.FromCurrencyCode, request.ToCurrencyCode, cancellationToken);
        return result;
    }
}
