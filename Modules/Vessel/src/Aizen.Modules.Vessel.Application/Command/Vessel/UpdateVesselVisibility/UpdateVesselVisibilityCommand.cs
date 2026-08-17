using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Enum;
using Aizen.Modules.Vessel.Abstraction.Response.Vessel;

namespace Aizen.Modules.Vessel.Application.Command.Vessel;

[DocumentationInfo("Update Vessel Visibility Command", "Carries the payload required to change a vessel's visibility setting.")]
public sealed class UpdateVesselVisibilityCommand : AizenCommand<UpdateVesselVisibilityResponse>
{
    public long VesselId { get; }
    public VesselVisibility Visibility { get; }

    public UpdateVesselVisibilityCommand(long vesselId, VesselVisibility visibility)
    {
        VesselId = vesselId;
        Visibility = visibility;
    }
}
