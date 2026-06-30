using Aizen.Modules.Vessel.Abstraction.Dto.Media;

namespace Aizen.Modules.Vessel.Abstraction.Response.Media;

[DocumentationInfo("Update vessel media response", "Returns the updated vessel media item.")]
public sealed record UpdateVesselMediaResponse(VesselMediaDto Media);
