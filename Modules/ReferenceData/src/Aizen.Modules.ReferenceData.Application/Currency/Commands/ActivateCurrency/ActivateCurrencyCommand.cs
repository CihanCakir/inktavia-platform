using Aizen.Core.CQRS.Message;

namespace Aizen.Modules.ReferenceData.Application.Currency.Commands;

public sealed class ActivateCurrencyCommand : AizenCommand<bool>
{
    public long Id { get; }

    public ActivateCurrencyCommand(long id)
    {
        Id = id;
    }
}
