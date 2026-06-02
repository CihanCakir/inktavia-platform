using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Dto.Status;

namespace Aizen.Modules.Vessel.Application.Query.Status;

public sealed class GetVesselStatusHistoryQuery : AizenQuery<IReadOnlyList<VesselStatusHistoryDto>>
{
    public long VesselId { get; }

    public GetVesselStatusHistoryQuery(long vesselId)
    {
        VesselId = vesselId;
    }
}
