using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Dto.Vessel;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Abstraction.Request.Vessel;

namespace Aizen.Modules.Vessel.Application.Command.Vessel;

[DocumentationInfo("Update Vessel Command", "Carries the payload required to update a vessel's profile fields.")]
public sealed class UpdateVesselCommand : AizenCommand<VesselDto>
{
    public long VesselId { get; }
    public UpdateVesselRequest Request { get; }

    public UpdateVesselCommand(long vesselId, UpdateVesselRequest request)
    {
        VesselId = vesselId;
        Request = request;
    }
}
