using Aizen.Core.CQRS.Message;

namespace Aizen.Modules.ReferenceData.Application.Currency.Commands;

public sealed class DeactivateCurrencyCommand : AizenCommand<bool>
{
    public long Id { get; }

    public DeactivateCurrencyCommand(long id)
    {
        Id = id;
    }
}
