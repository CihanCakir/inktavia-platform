using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Dto.Document;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Abstraction.Request.Document;

namespace Aizen.Modules.Vessel.Application.Command.Document;

[DocumentationInfo("Add Vessel Document Command", "Carries the payload required to attach a document to a vessel.")]
public sealed class AddVesselDocumentCommand : AizenCommand<VesselDocumentDto>
{
    public long VesselId { get; }
    public AddVesselDocumentRequest Request { get; }
    public long RequestingUserId { get; }

    public AddVesselDocumentCommand(long vesselId, AddVesselDocumentRequest request, long requestingUserId)
    {
        VesselId = vesselId;
        Request = request;
        RequestingUserId = requestingUserId;
    }
}
