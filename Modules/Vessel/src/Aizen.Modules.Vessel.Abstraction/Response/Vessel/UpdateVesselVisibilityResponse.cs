using Aizen.Modules.Vessel.Abstraction.Enum;
using Aizen.Modules.Vessel.Abstraction.Model;

namespace Aizen.Modules.Vessel.Abstraction.Response.Vessel;

[DocumentationInfo("Update vessel visibility response", "Returns the new visibility setting of the vessel.")]
public sealed record UpdateVesselVisibilityResponse(long VesselId, VesselVisibility Visibility);
