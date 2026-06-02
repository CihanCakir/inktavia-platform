using Aizen.Core.CQRS.Message;

namespace Aizen.Modules.ReferenceData.Application.Lookup.Commands;

public sealed class ActivateLookupItemCommand : AizenCommand<bool>
{
    public long Id { get; }

    public ActivateLookupItemCommand(long id)
    {
        Id = id;
    }
}
