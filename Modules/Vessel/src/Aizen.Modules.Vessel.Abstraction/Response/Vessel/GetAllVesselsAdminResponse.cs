using Aizen.Modules.Vessel.Abstraction.Dto.Vessel;
using Aizen.Modules.Vessel.Abstraction.Model;
using MiniUow.Paging;

namespace Aizen.Modules.Vessel.Abstraction.Response.Vessel;

[DocumentationInfo("Get all vessels admin response", "Returns a paged list of all vessels for admin use.")]
public sealed record GetAllVesselsAdminResponse(IPaginate<VesselListItemDto> Vessels);
