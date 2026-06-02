using Aizen.Core.CQRS.Message;

namespace Aizen.Modules.ReferenceData.Application.Lookup.Commands;

public sealed class ActivateLookupGroupCommand : AizenCommand<bool>
{
    public long Id { get; }

    public ActivateLookupGroupCommand(long id)
    {
        Id = id;
    }
}
