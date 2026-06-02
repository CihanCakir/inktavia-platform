using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Enum;
using Aizen.Modules.Vessel.Abstraction.Model;

namespace Aizen.Modules.Vessel.Application.Command.Document;

[DocumentationInfo("Update Vessel Document Status Command", "Carries the payload required to change a vessel document's status.")]
public sealed class UpdateVesselDocumentStatusCommand : AizenCommand<bool>
{
    public long VesselId { get; }
    public long DocumentId { get; }
    public VesselDocumentStatus Status { get; }
    public long RequestingUserId { get; }

    public UpdateVesselDocumentStatusCommand(long vesselId, long documentId, VesselDocumentStatus status, long requestingUserId)
    {
        VesselId = vesselId;
        DocumentId = documentId;
        Status = status;
        RequestingUserId = requestingUserId;
    }
}
