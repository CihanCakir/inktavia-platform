using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Response.Document;

namespace Aizen.Bff.AdminPanel.Application.AdminVessels.Command;

public sealed class RemoveVesselDocumentCommand : AizenCommand<RemoveVesselDocumentResponse>
{
    public long VesselId { get; }
    public long DocumentId { get; }
    public string UserToken { get; }
    public RemoveVesselDocumentCommand(long vesselId, long documentId, string userToken)
    {
        VesselId = vesselId;
        DocumentId = documentId;
        UserToken = userToken;
    }
}
