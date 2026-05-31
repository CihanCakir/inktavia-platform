using Aizen.Core.CQRS.Message;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupGroup;
using Aizen.Modules.ReferenceData.Abstraction.Request.LookupGroup;

namespace Aizen.Modules.ReferenceData.Application.Lookup.Commands;

public sealed class CreateLookupGroupCommand : AizenCommand<LookupGroupDto>
{
    public CreateLookupGroupRequest Request { get; }

    public CreateLookupGroupCommand(CreateLookupGroupRequest request)
    {
        Request = request;
    }
}
