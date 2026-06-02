using Aizen.Modules.Vessel.Abstraction.Dto.Specification;
using Aizen.Modules.Vessel.Abstraction.Model;

namespace Aizen.Modules.Vessel.Abstraction.Response.Specification;

[DocumentationInfo("Get vessel specification response", "Returns the vessel's physical specification.")]
public sealed record GetVesselSpecificationResponse(VesselSpecificationDto? Specification);
