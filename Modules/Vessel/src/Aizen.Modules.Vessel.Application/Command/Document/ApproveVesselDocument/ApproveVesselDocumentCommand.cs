using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Response.Document;

namespace Aizen.Modules.Vessel.Application.Command.Document;

[DocumentationInfo("Approve Vessel Document Command", "Marks a vessel document as approved (sets ApprovedAt / ApprovedByUserId). Idempotent — re-approve is a no-op.")]
public sealed class ApproveVesselDocumentCommand : AizenCommand<ApproveVesselDocumentResponse>
{
    public long VesselId { get; }
    public long DocumentId { get; }

    public ApproveVesselDocumentCommand(long vesselId, long documentId)
    {
        VesselId = vesselId;
        DocumentId = documentId;
    }
}
