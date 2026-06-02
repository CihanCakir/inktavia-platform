using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Dto.Media;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Abstraction.Request.Media;

namespace Aizen.Modules.Vessel.Application.Command.Media;

[DocumentationInfo("Add Vessel Media Command", "Carries the payload required to attach media to a vessel.")]
public sealed class AddVesselMediaCommand : AizenCommand<VesselMediaDto>
{
    public long VesselId { get; }
    public AddVesselMediaRequest Request { get; }

    public AddVesselMediaCommand(long vesselId, AddVesselMediaRequest request)
    {
        VesselId = vesselId;
        Request = request;
    }
}
