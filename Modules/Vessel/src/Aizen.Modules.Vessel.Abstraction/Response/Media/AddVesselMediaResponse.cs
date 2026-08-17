using Aizen.Modules.Vessel.Abstraction.Dto.Media;

namespace Aizen.Modules.Vessel.Abstraction.Response.Media;

[DocumentationInfo("Add vessel media response", "Returns the newly added vessel media item.")]
public sealed record AddVesselMediaResponse(VesselMediaDto Media);
