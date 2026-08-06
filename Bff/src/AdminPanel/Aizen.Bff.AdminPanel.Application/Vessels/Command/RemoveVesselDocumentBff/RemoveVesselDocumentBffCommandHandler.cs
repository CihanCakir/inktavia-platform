using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Vessel.Abstraction.Response.Document;

namespace Aizen.Bff.AdminPanel.Application.Vessels.Command;

[DocumentationInfo("Remove vessel document command handler", "Removes a document from a vessel via the Vessel module.")]
public sealed class RemoveVesselDocumentBffCommandHandler
    : AizenCommandHandler<RemoveVesselDocumentBffCommand, RemoveVesselDocumentResponse>
{
    private readonly IVesselRemoteCall _vessel;

    public RemoveVesselDocumentBffCommandHandler(IVesselRemoteCall vessel)
    {
        _vessel = vessel;
    }

    public override async Task<RemoveVesselDocumentResponse?> Handle(
        RemoveVesselDocumentBffCommand request, CancellationToken cancellationToken)
    {

        var result = await _vessel.RemoveVesselDocument(request.VesselId, request.DocumentId);
        return result.Body;
    }
}
