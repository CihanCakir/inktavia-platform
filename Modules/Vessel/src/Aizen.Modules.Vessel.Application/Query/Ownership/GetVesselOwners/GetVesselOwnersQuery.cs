using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Dto.Ownership;

namespace Aizen.Modules.Vessel.Application.Query.Ownership;

public sealed class GetVesselOwnersQuery : AizenPagedQuery<VesselOwnerDto>
{
    public long VesselId { get; }
    public int PageIndex { get; }
    public int PageSize { get; }

    public GetVesselOwnersQuery(long vesselId, int pageIndex = 0, int pageSize = 20)
    {
        VesselId = vesselId;
        PageIndex = pageIndex;
        PageSize = pageSize;
    }
}
