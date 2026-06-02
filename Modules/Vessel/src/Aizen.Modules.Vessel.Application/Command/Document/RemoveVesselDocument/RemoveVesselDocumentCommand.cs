using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Model;

namespace Aizen.Modules.Vessel.Application.Command.Document;

[DocumentationInfo("Remove Vessel Document Command", "Carries the payload required to deactivate a vessel document.")]
public sealed class RemoveVesselDocumentCommand : AizenCommand<bool>
{
    public long VesselId { get; }
    public long DocumentId { get; }

    public RemoveVesselDocumentCommand(long vesselId, long documentId)
    {
        VesselId = vesselId;
        DocumentId = documentId;
    }
}
