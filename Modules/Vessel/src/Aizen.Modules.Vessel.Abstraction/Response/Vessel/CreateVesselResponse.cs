using Aizen.Modules.Vessel.Abstraction.Dto.Vessel;

namespace Aizen.Modules.Vessel.Abstraction.Response.Vessel;

[DocumentationInfo("Create vessel response", "Returns the newly created vessel.")]
public sealed record CreateVesselResponse(VesselDto Vessel);
