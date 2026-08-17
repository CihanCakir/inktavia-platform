using Aizen.Modules.Vessel.Abstraction.Dto.Ownership;
using MiniUow.Paging;

namespace Aizen.Modules.Vessel.Abstraction.Response.Ownership;

[DocumentationInfo("Get vessel owners response", "Returns a paged list of vessel owners.")]
public sealed record GetVesselOwnersResponse(Paginate<VesselOwnerDto> Owners);
