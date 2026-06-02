using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Dto.Location;

namespace Aizen.Modules.Vessel.Application.Query.Location;

public sealed class GetCurrentVesselLocationQuery : AizenQuery<VesselLocationSnapshotDto?>
{
    public long VesselId { get; }

    public GetCurrentVesselLocationQuery(long vesselId)
    {
        VesselId = vesselId;
    }
}
