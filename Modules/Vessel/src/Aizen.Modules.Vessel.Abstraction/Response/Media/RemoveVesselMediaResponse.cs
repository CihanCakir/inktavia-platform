
namespace Aizen.Modules.Vessel.Abstraction.Response.Media;

[DocumentationInfo("Remove vessel media response", "Returns the IDs confirming the media removal.")]
public sealed record RemoveVesselMediaResponse(long VesselId, long MediaId);
