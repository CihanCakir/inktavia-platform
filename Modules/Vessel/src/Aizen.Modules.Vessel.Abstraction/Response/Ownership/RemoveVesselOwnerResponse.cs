
namespace Aizen.Modules.Vessel.Abstraction.Response.Ownership;

[DocumentationInfo("Remove vessel owner response", "Returns the IDs of the removed owner record.")]
public sealed record RemoveVesselOwnerResponse(long VesselId, long OwnerId);
