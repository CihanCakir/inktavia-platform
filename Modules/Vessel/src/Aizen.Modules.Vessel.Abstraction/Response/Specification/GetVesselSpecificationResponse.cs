using Aizen.Modules.Vessel.Abstraction.Dto.Specification;

namespace Aizen.Modules.Vessel.Abstraction.Response.Specification;

[DocumentationInfo("Get vessel specification response", "Returns the vessel's physical specification.")]
public sealed record GetVesselSpecificationResponse(VesselSpecificationDto? Specification);
