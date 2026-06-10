using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Vessel.Abstraction.Response.Document;
using Aizen.Bff.AdminPanel.Application.Common.Services;

namespace Aizen.Bff.AdminPanel.Application.AdminVessels.Command;

[DocumentationInfo("Remove vessel document command handler", "Removes a document from a vessel via the Vessel module.")]
public sealed class RemoveVesselDocumentCommandHandler
    : AizenCommandHandler<RemoveVesselDocumentCommand, RemoveVesselDocumentResponse>
{
    private readonly IVesselAdminBffRemoteCall _vessel;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;

    public RemoveVesselDocumentCommandHandler(IVesselAdminBffRemoteCall vessel,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider)
    {
        _vessel = vessel;
        _serviceTokenProvider = serviceTokenProvider;
    }

    public override async Task<RemoveVesselDocumentResponse?> Handle(
        RemoveVesselDocumentCommand request, CancellationToken cancellationToken)
    {
        var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(cancellationToken);
        var authHeader = $"Bearer {serviceToken}";

        var result = await _vessel.RemoveVesselDocument(request.VesselId, request.DocumentId, authHeader, request.UserToken);
        return result.Body;
    }
}
