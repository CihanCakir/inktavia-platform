using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Response.Specification;

namespace Aizen.Modules.Vessel.Application.Query.Specification;

public sealed class GetVesselSpecificationQuery : AizenQuery<GetVesselSpecificationResponse>
{
    public long VesselId { get; }

    public GetVesselSpecificationQuery(long vesselId)
    {
        VesselId = vesselId;
    }
}
