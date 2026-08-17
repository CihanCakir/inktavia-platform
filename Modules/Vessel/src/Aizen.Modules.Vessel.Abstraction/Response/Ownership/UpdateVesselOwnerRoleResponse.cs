using Aizen.Modules.Vessel.Abstraction.Dto.Ownership;

namespace Aizen.Modules.Vessel.Abstraction.Response.Ownership;

[DocumentationInfo("Update vessel owner role response", "Returns the updated vessel owner record.")]
public sealed record UpdateVesselOwnerRoleResponse(VesselOwnerDto Owner);
