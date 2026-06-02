using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Dto.Engine;

namespace Aizen.Modules.Vessel.Application.Query.Engine;

public sealed class GetVesselEnginesQuery : AizenQuery<IReadOnlyList<VesselEngineDto>>
{
    public long VesselId { get; }

    public GetVesselEnginesQuery(long vesselId)
    {
        VesselId = vesselId;
    }
}
