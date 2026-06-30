using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Request.Vessel;
using Aizen.Modules.Vessel.Abstraction.Response.Vessel;

namespace Aizen.Modules.Vessel.Application.Command.Vessel;

[DocumentationInfo("Create Vessel Command", "Carries the payload required to create a new vessel profile.")]
public sealed class CreateVesselCommand : AizenCommand<CreateVesselResponse>
{
    public CreateVesselRequest Request { get; }

    public CreateVesselCommand(CreateVesselRequest request)
    {
        Request = request;
    }
}
