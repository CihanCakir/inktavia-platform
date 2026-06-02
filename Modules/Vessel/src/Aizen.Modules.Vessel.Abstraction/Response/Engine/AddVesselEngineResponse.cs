using Aizen.Modules.Vessel.Abstraction.Dto.Engine;
using Aizen.Modules.Vessel.Abstraction.Model;

namespace Aizen.Modules.Vessel.Abstraction.Response.Engine;

[DocumentationInfo("Add vessel engine response", "Returns the newly added vessel engine.")]
public sealed record AddVesselEngineResponse(VesselEngineDto Engine);
