using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Model;

namespace Aizen.Modules.Vessel.Application.Command.Specification;

[DocumentationInfo("Remove Vessel Specification Command", "Carries the payload required to delete a vessel's specification record.")]
public sealed class RemoveVesselSpecificationCommand : AizenCommand<bool>
{
    public long VesselId { get; }
    public long RequestingUserId { get; }

    public RemoveVesselSpecificationCommand(long vesselId, long requestingUserId)
    {
        VesselId = vesselId;
        RequestingUserId = requestingUserId;
    }
}
