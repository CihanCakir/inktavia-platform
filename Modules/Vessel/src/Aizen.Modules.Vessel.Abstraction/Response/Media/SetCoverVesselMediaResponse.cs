
namespace Aizen.Modules.Vessel.Abstraction.Response.Media;

[DocumentationInfo("Set cover vessel media response", "Returns the IDs confirming the cover media change.")]
public sealed record SetCoverVesselMediaResponse(long VesselId, long MediaId);
