using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Request.Vessel;
using Aizen.Modules.Vessel.Abstraction.Response.Vessel;

namespace Aizen.Modules.Vessel.Application.Command.Vessel;

[DocumentationInfo("Create Admin Vessel Command", "Admin command to create a vessel with an explicitly specified owner user ID.")]
public sealed class CreateAdminVesselCommand : AizenCommand<CreateVesselResponse>
{
    public CreateAdminVesselRequest Request { get; }

    public CreateAdminVesselCommand(CreateAdminVesselRequest request)
    {
        Request = request;
    }
}
