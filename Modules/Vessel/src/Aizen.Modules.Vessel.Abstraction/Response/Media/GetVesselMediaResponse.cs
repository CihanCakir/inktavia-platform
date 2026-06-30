using Aizen.Modules.Vessel.Abstraction.Dto.Media;
using MiniUow.Paging;

namespace Aizen.Modules.Vessel.Abstraction.Response.Media;

[DocumentationInfo("Get vessel media response", "Returns a paged list of vessel media items.")]
public sealed record GetVesselMediaResponse(Paginate<VesselMediaDto> Media);
