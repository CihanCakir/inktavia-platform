using Aizen.Modules.Vessel.Abstraction.Model;

namespace Aizen.Modules.Vessel.Abstraction.Response.Engine;

[DocumentationInfo("Set primary vessel engine response", "Returns the IDs confirming the primary engine change.")]
public sealed record SetPrimaryVesselEngineResponse(long VesselId, long EngineId);
