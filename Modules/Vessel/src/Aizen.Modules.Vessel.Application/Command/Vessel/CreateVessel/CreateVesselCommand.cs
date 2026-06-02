using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Dto.Vessel;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Abstraction.Request.Vessel;

namespace Aizen.Modules.Vessel.Application.Command.Vessel;

[DocumentationInfo("Create Vessel Command", "Carries the payload required to create a new vessel profile.")]
public sealed class CreateVesselCommand : AizenCommand<VesselDto>
{
    public CreateVesselRequest Request { get; }
    public long RequestingUserId { get; }

    public CreateVesselCommand(CreateVesselRequest request, long requestingUserId)
    {
        Request = request;
        RequestingUserId = requestingUserId;
    }
}
