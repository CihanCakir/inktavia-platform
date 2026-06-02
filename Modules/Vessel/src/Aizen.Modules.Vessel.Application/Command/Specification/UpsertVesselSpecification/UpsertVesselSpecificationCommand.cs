using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Dto.Specification;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Abstraction.Request.Specification;

namespace Aizen.Modules.Vessel.Application.Command.Specification;

[DocumentationInfo("Upsert Vessel Specification Command", "Creates or updates the physical specification for a vessel.")]
public sealed class UpsertVesselSpecificationCommand : AizenCommand<VesselSpecificationDto>
{
    public long VesselId { get; }
    public UpsertVesselSpecificationRequest Request { get; }
    public long RequestingUserId { get; }

    public UpsertVesselSpecificationCommand(long vesselId, UpsertVesselSpecificationRequest request, long requestingUserId)
    {
        VesselId = vesselId;
        Request = request;
        RequestingUserId = requestingUserId;
    }
}
