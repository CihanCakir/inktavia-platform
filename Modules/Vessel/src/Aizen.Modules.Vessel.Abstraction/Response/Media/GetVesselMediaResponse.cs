using Aizen.Modules.Vessel.Abstraction.Dto.Media;
using Aizen.Modules.Vessel.Abstraction.Model;
using MiniUow.Paging;

namespace Aizen.Modules.Vessel.Abstraction.Response.Media;

[DocumentationInfo("Get vessel media response", "Returns a paged list of vessel media items.")]
public sealed record GetVesselMediaResponse(IPaginate<VesselMediaDto> Media);
