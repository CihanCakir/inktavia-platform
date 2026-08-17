using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Response.Status;

namespace Aizen.Modules.Vessel.Application.Query.Vessel;

[DocumentationInfo("Get vessel status history by owner query", "Returns all status change events for vessels owned by a specific user, used in activity feed aggregation.")]
public sealed class GetVesselStatusHistoryByOwnerQuery : AizenQuery<GetVesselStatusHistoryByOwnerResponse>
{
    public long OwnerUserId { get; }
    public int PageSize { get; }

    public GetVesselStatusHistoryByOwnerQuery(long ownerUserId, int pageSize = 200)
    {
        OwnerUserId = ownerUserId;
        PageSize = Math.Clamp(pageSize, 1, 500);
    }
}
