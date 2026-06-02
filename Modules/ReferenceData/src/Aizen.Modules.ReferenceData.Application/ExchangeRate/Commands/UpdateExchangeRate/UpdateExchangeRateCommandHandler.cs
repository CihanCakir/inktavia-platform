using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.ExchangeRate;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.ExchangeRate.Commands;

public sealed class UpdateExchangeRateCommandHandler : AizenCommandHandler<UpdateExchangeRateCommand, ExchangeRateDto>
{
    private readonly IExchangeRateReferenceService _service;
    private readonly IReferenceDataCacheInvalidationService _invalidation;

    public UpdateExchangeRateCommandHandler(IExchangeRateReferenceService service, IReferenceDataCacheInvalidationService invalidation)
    {
        _service = service;
        _invalidation = invalidation;
    }

    public override async Task<ExchangeRateDto?> Handle(UpdateExchangeRateCommand request, CancellationToken cancellationToken)
    {
        var result = await _service.UpdateRateAsync(request.Request, cancellationToken);
        await _invalidation.InvalidateExchangeRateAsync(request.Request.FromCurrencyCode, request.Request.ToCurrencyCode, cancellationToken);
        return result;
    }
}
