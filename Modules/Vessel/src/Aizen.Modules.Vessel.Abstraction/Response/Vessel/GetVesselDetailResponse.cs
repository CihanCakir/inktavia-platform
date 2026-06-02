using Aizen.Modules.Vessel.Abstraction.Dto.Vessel;
using Aizen.Modules.Vessel.Abstraction.Model;

namespace Aizen.Modules.Vessel.Abstraction.Response.Vessel;

[DocumentationInfo("Get vessel detail response", "Returns full vessel detail including sub-entities.")]
public sealed record GetVesselDetailResponse(VesselDetailDto? Vessel);
