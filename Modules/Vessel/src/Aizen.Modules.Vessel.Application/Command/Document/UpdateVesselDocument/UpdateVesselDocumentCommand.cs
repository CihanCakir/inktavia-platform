using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Abstraction.Request.Document;
using Aizen.Modules.Vessel.Abstraction.Response.Document;

namespace Aizen.Modules.Vessel.Application.Command.Document;

[DocumentationInfo("Update Vessel Document Command", "Carries the payload required to update a vessel document's metadata.")]
public sealed class UpdateVesselDocumentCommand : AizenCommand<UpdateVesselDocumentResponse>
{
    public long VesselId { get; }
    public long DocumentId { get; }
    public UpdateVesselDocumentRequest Request { get; }

    public UpdateVesselDocumentCommand(long vesselId, long documentId, UpdateVesselDocumentRequest request)
    {
        VesselId = vesselId;
        DocumentId = documentId;
        Request = request;
    }
}
