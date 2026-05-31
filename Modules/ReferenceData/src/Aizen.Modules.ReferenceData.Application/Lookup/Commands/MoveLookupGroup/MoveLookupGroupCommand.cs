using Aizen.Core.CQRS.Message;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupGroup;
using Aizen.Modules.ReferenceData.Abstraction.Request.LookupGroup;

namespace Aizen.Modules.ReferenceData.Application.Lookup.Commands;

public sealed class MoveLookupGroupCommand : AizenCommand<LookupGroupTreeDto>
{
    public MoveLookupGroupRequest Request { get; }

    public MoveLookupGroupCommand(MoveLookupGroupRequest request)
    {
        Request = request;
    }
}
