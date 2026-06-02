using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Abstraction.Response.Ownership;

namespace Aizen.Modules.Vessel.Application.Command.Ownership;

[DocumentationInfo("Accept Vessel Ownership Invitation Command", "Carries the payload required to accept a vessel ownership invitation.")]
public sealed class AcceptVesselOwnershipInvitationCommand : AizenCommand<AcceptVesselOwnershipInvitationResponse>
{
    public long VesselId { get; }

    public AcceptVesselOwnershipInvitationCommand(long vesselId)
    {
        VesselId = vesselId;
    }
}
