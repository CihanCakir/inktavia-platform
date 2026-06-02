using Aizen.Modules.Vessel.Abstraction.Dto.Vessel;
using Aizen.Modules.Vessel.Abstraction.Model;

namespace Aizen.Modules.Vessel.Abstraction.Response.Vessel;

[DocumentationInfo("Get vessel by id response", "Returns a vessel matched by its internal ID.")]
public sealed record GetVesselByIdResponse(VesselDto? Vessel);
