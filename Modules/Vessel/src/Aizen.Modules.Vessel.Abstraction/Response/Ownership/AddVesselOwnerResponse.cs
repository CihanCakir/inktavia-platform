using Aizen.Modules.Vessel.Abstraction.Dto.Ownership;

namespace Aizen.Modules.Vessel.Abstraction.Response.Ownership;

[DocumentationInfo("Add vessel owner response", "Returns the newly added vessel owner.")]
public sealed record AddVesselOwnerResponse(VesselOwnerDto Owner);
