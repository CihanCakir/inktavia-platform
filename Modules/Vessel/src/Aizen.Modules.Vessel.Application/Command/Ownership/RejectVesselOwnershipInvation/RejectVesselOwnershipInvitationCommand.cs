using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Response.Ownership;

namespace Aizen.Modules.Vessel.Application.Command.Ownership;

[DocumentationInfo("Reject Vessel Ownership Invitation Command", "Carries the payload required to reject a vessel ownership invitation.")]
public sealed class RejectVesselOwnershipInvitationCommand : AizenCommand<RejectVesselOwnershipInvitationResponse>
{
    public long VesselId { get; }

    public RejectVesselOwnershipInvitationCommand(long vesselId)
    {
        VesselId = vesselId;
    }
}
