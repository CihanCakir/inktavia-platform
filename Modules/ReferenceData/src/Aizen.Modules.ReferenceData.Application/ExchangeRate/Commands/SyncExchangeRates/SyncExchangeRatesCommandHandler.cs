using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.ExchangeRate.Commands;

public sealed class SyncExchangeRatesCommandHandler : AizenCommandHandler<SyncExchangeRatesCommand, bool>
{
    private readonly IExchangeRateReferenceService _service;

    public SyncExchangeRatesCommandHandler(IExchangeRateReferenceService service)
    {
        _service = service;
    }

    public override async Task<bool> Handle(SyncExchangeRatesCommand request, CancellationToken cancellationToken)
    {
        await _service.SyncRatesAsync(request.Requests, cancellationToken);
        return true;
    }
}
