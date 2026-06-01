using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.ExchangeRate.Commands;

public sealed class SyncExchangeRatesCommandHandler : AizenCommandHandler<SyncExchangeRatesCommand, bool>
{
    private readonly IExchangeRateReferenceService _service;
    private readonly IReferenceDataCacheInvalidationService _invalidation;

    public SyncExchangeRatesCommandHandler(IExchangeRateReferenceService service, IReferenceDataCacheInvalidationService invalidation)
    {
        _service = service;
        _invalidation = invalidation;
    }

    public override async Task<bool> Handle(SyncExchangeRatesCommand request, CancellationToken cancellationToken)
    {
        await _service.SyncRatesAsync(request.Requests, cancellationToken);
        foreach (var r in request.Requests)
            await _invalidation.InvalidateExchangeRateAsync(r.FromCurrencyCode, r.ToCurrencyCode, cancellationToken);
        return true;
    }
}
