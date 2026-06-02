using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Abstraction.Request.Specification;
using Aizen.Modules.Vessel.Abstraction.Response.Specification;

namespace Aizen.Modules.Vessel.Application.Command.Specification;

[DocumentationInfo("Upsert Vessel Specification Command", "Creates or updates the physical specification for a vessel.")]
public sealed class UpsertVesselSpecificationCommand : AizenCommand<UpsertVesselSpecificationResponse>
{
    public long VesselId { get; }
    public UpsertVesselSpecificationRequest Request { get; }

    public UpsertVesselSpecificationCommand(long vesselId, UpsertVesselSpecificationRequest request)
    {
        VesselId = vesselId;
        Request = request;
    }
}
