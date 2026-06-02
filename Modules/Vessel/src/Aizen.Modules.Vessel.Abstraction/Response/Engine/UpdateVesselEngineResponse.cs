using Aizen.Modules.Vessel.Abstraction.Dto.Engine;
using Aizen.Modules.Vessel.Abstraction.Model;

namespace Aizen.Modules.Vessel.Abstraction.Response.Engine;

[DocumentationInfo("Update vessel engine response", "Returns the updated vessel engine.")]
public sealed record UpdateVesselEngineResponse(VesselEngineDto Engine);
