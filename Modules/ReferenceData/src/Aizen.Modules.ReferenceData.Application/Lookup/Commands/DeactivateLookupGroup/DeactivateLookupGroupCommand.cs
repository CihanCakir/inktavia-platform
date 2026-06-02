using Aizen.Core.CQRS.Message;

namespace Aizen.Modules.ReferenceData.Application.Lookup.Commands;

public sealed class DeactivateLookupGroupCommand : AizenCommand<bool>
{
    public long Id { get; }

    public DeactivateLookupGroupCommand(long id)
    {
        Id = id;
    }
}
