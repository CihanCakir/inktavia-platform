using Aizen.Modules.Vessel.Abstraction.Dto.Status;
using MiniUow.Paging;

namespace Aizen.Modules.Vessel.Abstraction.Response.Status;

[DocumentationInfo("Get vessel status history response", "Returns a paged list of vessel status change history.")]
public sealed record GetVesselStatusHistoryResponse(Paginate<VesselStatusHistoryDto> History);
