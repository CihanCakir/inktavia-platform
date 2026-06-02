using Aizen.Modules.Vessel.Abstraction.Dto.Vessel;
using Aizen.Modules.Vessel.Abstraction.Model;

namespace Aizen.Modules.Vessel.Abstraction.Response.Vessel;

[DocumentationInfo("Update vessel response", "Returns the updated vessel.")]
public sealed record UpdateVesselResponse(VesselDto Vessel);
