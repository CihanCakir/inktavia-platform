
namespace Aizen.Modules.Vessel.Abstraction.Response.Ownership;

[DocumentationInfo("Accept vessel ownership invitation response", "Returns the vessel and user IDs for the accepted invitation.")]
public sealed record AcceptVesselOwnershipInvitationResponse(long VesselId, long UserId);
