using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Model;

namespace Aizen.Modules.Vessel.Application.Command.Ownership;

[DocumentationInfo("Accept Vessel Ownership Invitation Command", "Carries the payload required to accept a vessel ownership invitation.")]
public sealed class AcceptVesselOwnershipInvitationCommand : AizenCommand<bool>
{
    public long VesselId { get; }
    public long UserId { get; }

    public AcceptVesselOwnershipInvitationCommand(long vesselId, long userId)
    {
        VesselId = vesselId;
        UserId = userId;
    }
}
