using Aizen.Modules.Vessel.Abstraction.Enum;
using Aizen.Modules.Vessel.Abstraction.Model;

namespace Aizen.Modules.Vessel.Abstraction.Response.Vessel;

[DocumentationInfo("Update vessel status response", "Returns the new operational status of the vessel.")]
public sealed record UpdateVesselStatusResponse(long VesselId, VesselStatus NewStatus);
