using Aizen.Modules.Vessel.Abstraction.Dto.Vessel;
using Aizen.Modules.Vessel.Abstraction.Model;
using MiniUow.Paging;

namespace Aizen.Modules.Vessel.Abstraction.Response.Vessel;

[DocumentationInfo("Get user vessels response", "Returns a paged list of vessels owned by the user.")]
public sealed record GetUserVesselsResponse(IPaginate<VesselListItemDto> Vessels);
