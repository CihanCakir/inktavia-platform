using Aizen.Core.CQRS.Message;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Currency;
using Aizen.Modules.ReferenceData.Abstraction.Request.Currency;

namespace Aizen.Modules.ReferenceData.Application.Currency.Commands;

public sealed class CreateCurrencyCommand : AizenCommand<CurrencyDto>
{
    public CreateCurrencyRequest Request { get; }

    public CreateCurrencyCommand(CreateCurrencyRequest request)
    {
        Request = request;
    }
}
