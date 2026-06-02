using Aizen.Modules.Vessel.Abstraction.Dto.Vessel;
using Aizen.Modules.Vessel.Abstraction.Model;

namespace Aizen.Modules.Vessel.Abstraction.Response.Vessel;

[DocumentationInfo("Create vessel response", "Returns the newly created vessel.")]
public sealed record CreateVesselResponse(VesselDto Vessel);
