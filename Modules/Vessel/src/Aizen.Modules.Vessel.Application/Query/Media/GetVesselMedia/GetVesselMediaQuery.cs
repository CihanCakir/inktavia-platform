using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Dto.Media;

namespace Aizen.Modules.Vessel.Application.Query.Media;

public sealed class GetVesselMediaQuery : AizenQuery<IReadOnlyList<VesselMediaDto>>
{
    public long VesselId { get; }

    public GetVesselMediaQuery(long vesselId)
    {
        VesselId = vesselId;
    }
}
