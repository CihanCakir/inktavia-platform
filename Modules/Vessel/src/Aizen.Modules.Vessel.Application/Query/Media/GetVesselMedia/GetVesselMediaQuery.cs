using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Dto.Media;

namespace Aizen.Modules.Vessel.Application.Query.Media;

public sealed class GetVesselMediaQuery : AizenPagedQuery<VesselMediaDto>
{
    public long VesselId { get; }
    public int PageIndex { get; }
    public int PageSize { get; }

    public GetVesselMediaQuery(long vesselId, int pageIndex = 0, int pageSize = 20)
    {
        VesselId = vesselId;
        PageIndex = pageIndex;
        PageSize = pageSize;
    }
}
