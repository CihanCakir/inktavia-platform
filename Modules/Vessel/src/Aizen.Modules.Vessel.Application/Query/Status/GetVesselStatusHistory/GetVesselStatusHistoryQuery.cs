using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Response.Status;

namespace Aizen.Modules.Vessel.Application.Query.Status;

public sealed class GetVesselStatusHistoryQuery : AizenQuery<GetVesselStatusHistoryResponse>
{
    public long VesselId { get; }
    public int PageIndex { get; }
    public int PageSize { get; }

    public GetVesselStatusHistoryQuery(long vesselId, int pageIndex = 0, int pageSize = 20)
    {
        VesselId = vesselId;
        PageIndex = pageIndex;
        PageSize = pageSize;
    }
}
