using Aizen.Modules.Vessel.Abstraction.Dto.Location;

namespace Aizen.Modules.Vessel.Abstraction.Response.Location;

[DocumentationInfo("Get current vessel location response", "Returns the vessel's current location snapshot.")]
public sealed record GetCurrentVesselLocationResponse(VesselLocationSnapshotDto? Location);
