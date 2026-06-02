using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Response.Location;

namespace Aizen.Modules.Vessel.Application.Query.Location;

public sealed class GetCurrentVesselLocationQuery : AizenQuery<GetCurrentVesselLocationResponse>
{
    public long VesselId { get; }

    public GetCurrentVesselLocationQuery(long vesselId)
    {
        VesselId = vesselId;
    }
}
