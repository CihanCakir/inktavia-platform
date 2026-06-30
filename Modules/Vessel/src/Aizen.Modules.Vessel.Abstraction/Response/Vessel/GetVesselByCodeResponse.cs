using Aizen.Modules.Vessel.Abstraction.Dto.Vessel;

namespace Aizen.Modules.Vessel.Abstraction.Response.Vessel;

[DocumentationInfo("Get vessel by code response", "Returns a vessel matched by its vessel code.")]
public sealed record GetVesselByCodeResponse(VesselDto? Vessel);
