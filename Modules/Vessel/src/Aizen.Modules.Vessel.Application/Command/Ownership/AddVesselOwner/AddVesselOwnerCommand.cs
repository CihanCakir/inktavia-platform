using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Abstraction.Request.Ownership;
using Aizen.Modules.Vessel.Abstraction.Response.Ownership;

namespace Aizen.Modules.Vessel.Application.Command.Ownership;

[DocumentationInfo("Add Vessel Owner Command", "Carries the payload required to invite a user as a vessel owner.")]
public sealed class AddVesselOwnerCommand : AizenCommand<AddVesselOwnerResponse>
{
    public long VesselId { get; }
    public AddVesselOwnerRequest Request { get; }

    public AddVesselOwnerCommand(long vesselId, AddVesselOwnerRequest request)
    {
        VesselId = vesselId;
        Request = request;
    }
}
