using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Dto.Engine;

namespace Aizen.Modules.Vessel.Application.Query.Engine;

public sealed class GetVesselEnginesQuery : AizenPagedQuery<VesselEngineDto>
{
    public long VesselId { get; }
    public int PageIndex { get; }
    public int PageSize { get; }

    public GetVesselEnginesQuery(long vesselId, int pageIndex = 0, int pageSize = 20)
    {
        VesselId = vesselId;
        PageIndex = pageIndex;
        PageSize = pageSize;
    }
}
