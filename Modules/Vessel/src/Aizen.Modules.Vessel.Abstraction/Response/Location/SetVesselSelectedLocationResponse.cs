using Aizen.Modules.Vessel.Abstraction.Dto.Location;

namespace Aizen.Modules.Vessel.Abstraction.Response.Location;

[DocumentationInfo("Set vessel selected location response", "Returns the stored selection (null when cleared).")]
public sealed record SetVesselSelectedLocationResponse(VesselSelectedLocationDto? SelectedLocation);
