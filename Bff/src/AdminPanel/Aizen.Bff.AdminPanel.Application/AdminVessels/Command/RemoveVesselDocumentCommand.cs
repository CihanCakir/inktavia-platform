using Aizen.Bff.AdminPanel.Application.Common;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Message;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Vessel.Abstraction.Response.Document;

namespace Aizen.Bff.AdminPanel.Application.AdminVessels.Command;

public sealed class RemoveVesselDocumentCommand : AizenCommand<RemoveVesselDocumentResponse>
{
    public long VesselId { get; }
    public long DocumentId { get; }
    public string Authorization { get; }
    public RemoveVesselDocumentCommand(long vesselId, long documentId, string authorization)
    {
        VesselId = vesselId;
        DocumentId = documentId;
        Authorization = authorization;
    }
}

[DocumentationInfo("Remove vessel document command handler", "Removes a document from a vessel via the Vessel module.")]
public sealed class RemoveVesselDocumentCommandHandler
    : AizenCommandHandler<RemoveVesselDocumentCommand, RemoveVesselDocumentResponse>
{
    private readonly IVesselAdminBffRemoteCall _vessel;

    public RemoveVesselDocumentCommandHandler(IVesselAdminBffRemoteCall vessel)
    {
        _vessel = vessel;
    }

    public override async Task<RemoveVesselDocumentResponse?> Handle(
        RemoveVesselDocumentCommand request, CancellationToken cancellationToken)
    {
        var result = await _vessel.RemoveVesselDocument(
            request.VesselId,
            request.DocumentId,
            request.Authorization);

        return result.Body;
    }
}
