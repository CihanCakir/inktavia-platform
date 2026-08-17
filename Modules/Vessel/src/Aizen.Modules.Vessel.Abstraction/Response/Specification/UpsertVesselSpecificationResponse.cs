using Aizen.Modules.Vessel.Abstraction.Dto.Specification;

namespace Aizen.Modules.Vessel.Abstraction.Response.Specification;

[DocumentationInfo("Upsert vessel specification response", "Returns the created or updated vessel specification.")]
public sealed record UpsertVesselSpecificationResponse(VesselSpecificationDto Specification);
