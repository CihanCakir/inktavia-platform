using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Request.Document;
using Aizen.Modules.Vessel.Abstraction.Response.Document;

namespace Aizen.Modules.Vessel.Application.Command.Document;

[DocumentationInfo("Add Vessel Document Command", "Carries the payload required to attach a document to a vessel.")]
public sealed class AddVesselDocumentCommand : AizenCommand<AddVesselDocumentResponse>
{
    public long VesselId { get; }
    public AddVesselDocumentRequest Request { get; }

    public AddVesselDocumentCommand(long vesselId, AddVesselDocumentRequest request)
    {
        VesselId = vesselId;
        Request = request;
    }
}
