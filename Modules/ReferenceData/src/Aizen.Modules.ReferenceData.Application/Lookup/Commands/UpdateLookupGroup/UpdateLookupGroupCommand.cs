using Aizen.Core.CQRS.Message;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupGroup;
using Aizen.Modules.ReferenceData.Abstraction.Request.LookupGroup;

namespace Aizen.Modules.ReferenceData.Application.Lookup.Commands;

public sealed class UpdateLookupGroupCommand : AizenCommand<LookupGroupDto>
{
    public long Id { get; }
    public UpdateLookupGroupRequest Request { get; }

    public UpdateLookupGroupCommand(long id, UpdateLookupGroupRequest request)
    {
        Id = id;
        Request = request;
    }
}
