
namespace Aizen.Modules.Vessel.Abstraction.Response.Ownership;

[DocumentationInfo("Reject vessel ownership invitation response", "Returns the vessel and user IDs for the rejected invitation.")]
public sealed record RejectVesselOwnershipInvitationResponse(long VesselId, long UserId);
