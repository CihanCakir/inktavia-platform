using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Dto.Specification;

namespace Aizen.Modules.Vessel.Application.Query.Specification;

public sealed class GetVesselSpecificationQuery : AizenQuery<VesselSpecificationDto?>
{
    public long VesselId { get; }

    public GetVesselSpecificationQuery(long vesselId)
    {
        VesselId = vesselId;
    }
}
