using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Dto.Vessel;

namespace Aizen.Modules.Vessel.Application.Query.Vessel;

public sealed class GetVesselByIdQuery : AizenQuery<VesselDto?>
{
    public long VesselId { get; }

    public GetVesselByIdQuery(long vesselId)
    {
        VesselId = vesselId;
    }
}
