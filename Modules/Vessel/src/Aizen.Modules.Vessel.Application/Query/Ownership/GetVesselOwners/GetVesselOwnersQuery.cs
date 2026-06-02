using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Dto.Ownership;

namespace Aizen.Modules.Vessel.Application.Query.Ownership;

public sealed class GetVesselOwnersQuery : AizenQuery<IReadOnlyList<VesselOwnerDto>>
{
    public long VesselId { get; }

    public GetVesselOwnersQuery(long vesselId)
    {
        VesselId = vesselId;
    }
}
