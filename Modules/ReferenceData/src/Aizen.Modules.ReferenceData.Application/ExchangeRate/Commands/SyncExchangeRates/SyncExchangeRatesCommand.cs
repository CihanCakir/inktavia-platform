using Aizen.Core.CQRS.Message;
using Aizen.Modules.ReferenceData.Abstraction.Request.ExchangeRate;

namespace Aizen.Modules.ReferenceData.Application.ExchangeRate.Commands;

public sealed class SyncExchangeRatesCommand : AizenCommand<bool>
{
    public IEnumerable<UpdateExchangeRateRequest> Requests { get; }

    public SyncExchangeRatesCommand(IEnumerable<UpdateExchangeRateRequest> requests)
    {
        Requests = requests;
    }
}
