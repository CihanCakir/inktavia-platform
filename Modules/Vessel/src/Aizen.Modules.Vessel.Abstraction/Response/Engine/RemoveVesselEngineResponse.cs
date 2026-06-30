
namespace Aizen.Modules.Vessel.Abstraction.Response.Engine;

[DocumentationInfo("Remove vessel engine response", "Returns the IDs confirming the engine removal.")]
public sealed record RemoveVesselEngineResponse(long VesselId, long EngineId);
