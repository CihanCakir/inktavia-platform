using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Response.Vessel;

namespace Aizen.Modules.Vessel.Application.Query.Vessel;

public sealed class GetVesselDetailQuery : AizenQuery<GetVesselDetailResponse>
{
    public long VesselId { get; }

    public GetVesselDetailQuery(long vesselId)
    {
        VesselId = vesselId;
    }
}
