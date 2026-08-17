using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Response.Document;

namespace Aizen.Bff.AdminPanel.Application.Vessels.Command;

public sealed class RemoveVesselDocumentBffCommand : AizenCommand<RemoveVesselDocumentResponse>
{
    public long VesselId { get; }
    public long DocumentId { get; }
    public RemoveVesselDocumentBffCommand(long vesselId, long documentId)
    {
        VesselId = vesselId;
        DocumentId = documentId;
    }
}
