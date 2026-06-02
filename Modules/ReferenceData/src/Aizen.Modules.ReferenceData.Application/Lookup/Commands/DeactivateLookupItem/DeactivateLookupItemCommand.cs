using Aizen.Core.CQRS.Message;

namespace Aizen.Modules.ReferenceData.Application.Lookup.Commands;

public sealed class DeactivateLookupItemCommand : AizenCommand<bool>
{
    public long Id { get; }

    public DeactivateLookupItemCommand(long id)
    {
        Id = id;
    }
}
