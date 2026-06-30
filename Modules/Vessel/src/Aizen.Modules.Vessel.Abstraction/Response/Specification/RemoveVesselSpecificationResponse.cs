
namespace Aizen.Modules.Vessel.Abstraction.Response.Specification;

[DocumentationInfo("Remove vessel specification response", "Returns the ID of the vessel whose specification was removed.")]
public sealed record RemoveVesselSpecificationResponse(long VesselId);
