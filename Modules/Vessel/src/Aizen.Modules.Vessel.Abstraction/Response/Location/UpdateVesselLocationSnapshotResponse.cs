using Aizen.Modules.Vessel.Abstraction.Dto.Location;

namespace Aizen.Modules.Vessel.Abstraction.Response.Location;

[DocumentationInfo("Update vessel location snapshot response", "Returns the new current location snapshot.")]
public sealed record UpdateVesselLocationSnapshotResponse(VesselLocationSnapshotDto Snapshot);
