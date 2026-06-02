using Aizen.Core.CQRS.Message;
using Aizen.Modules.ReferenceData.Abstraction.Dto.ExchangeRate;
using Aizen.Modules.ReferenceData.Abstraction.Request.ExchangeRate;

namespace Aizen.Modules.ReferenceData.Application.ExchangeRate.Commands;

public sealed class UpdateExchangeRateCommand : AizenCommand<ExchangeRateDto>
{
    public UpdateExchangeRateRequest Request { get; }

    public UpdateExchangeRateCommand(UpdateExchangeRateRequest request)
    {
        Request = request;
    }
}
