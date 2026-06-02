using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Dto.Vessel;

namespace Aizen.Modules.Vessel.Application.Query.Vessel;

public sealed class GetVesselDetailQuery : AizenQuery<VesselDetailDto?>
{
    public long VesselId { get; }

    public GetVesselDetailQuery(long vesselId)
    {
        VesselId = vesselId;
    }
}
