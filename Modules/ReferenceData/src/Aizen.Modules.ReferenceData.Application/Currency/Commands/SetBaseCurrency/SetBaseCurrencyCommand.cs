using Aizen.Core.CQRS.Message;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Currency;

namespace Aizen.Modules.ReferenceData.Application.Currency.Commands;

public sealed class SetBaseCurrencyCommand : AizenCommand<CurrencyDto>
{
    public long Id { get; }

    public SetBaseCurrencyCommand(long id)
    {
        Id = id;
    }
}
