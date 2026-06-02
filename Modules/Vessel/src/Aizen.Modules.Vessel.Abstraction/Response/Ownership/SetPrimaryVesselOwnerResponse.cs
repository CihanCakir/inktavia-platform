using Aizen.Modules.Vessel.Abstraction.Model;

namespace Aizen.Modules.Vessel.Abstraction.Response.Ownership;

[DocumentationInfo("Set primary vessel owner response", "Returns the IDs confirming the primary owner change.")]
public sealed record SetPrimaryVesselOwnerResponse(long VesselId, long OwnerId);
