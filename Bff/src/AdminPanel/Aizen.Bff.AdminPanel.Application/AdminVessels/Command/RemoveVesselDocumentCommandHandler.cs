using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Vessel.Abstraction.Response.Document;

namespace Aizen.Bff.AdminPanel.Application.AdminVessels.Command;

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

        var result = await _vessel.RemoveVesselDocument(request.VesselId, request.DocumentId);
        return result.Body;
    }
}
