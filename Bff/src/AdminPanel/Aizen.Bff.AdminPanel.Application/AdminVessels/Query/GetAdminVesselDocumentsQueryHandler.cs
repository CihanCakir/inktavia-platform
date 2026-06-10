using Aizen.Bff.AdminPanel.Application.AdminVessels.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.CQRS.Handler;
using Aizen.Bff.AdminPanel.Application.Common.Services;

namespace Aizen.Bff.AdminPanel.Application.AdminVessels.Query;

[DocumentationInfo("Get admin vessel documents query handler", "Fetches vessel detail, documents and owners in parallel for the admin vessel detail screen.")]
public sealed class GetAdminVesselDocumentsQueryHandler
    : AizenQueryHandler<GetAdminVesselDocumentsQuery, AdminVesselDocumentsResponse>
{
    private readonly IVesselAdminBffRemoteCall _vessel;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;

    public GetAdminVesselDocumentsQueryHandler(IVesselAdminBffRemoteCall vessel,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider)
    {
        _vessel = vessel;
        _serviceTokenProvider = serviceTokenProvider;
    }

    public override async Task<AdminVesselDocumentsResponse?> Handle(
        GetAdminVesselDocumentsQuery request, CancellationToken cancellationToken)
    {
        var response = new AdminVesselDocumentsResponse();

        var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(cancellationToken);
        var authHeader = $"Bearer {serviceToken}";

        var detailTask = _vessel.GetVesselById(request.VesselId, authHeader, request.UserToken);
        var docsTask = _vessel.GetVesselDocuments(request.VesselId, authHeader, request.UserToken);
        var ownersTask = _vessel.GetVesselOwners(request.VesselId, authHeader, request.UserToken);

        await Task.WhenAll(
            detailTask.ContinueWith(_ => { }),
            docsTask.ContinueWith(_ => { }),
            ownersTask.ContinueWith(_ => { }));

        if (detailTask.IsCompletedSuccessfully)
            response.Vessel = detailTask.Result.Body;
        else
            response.Warnings.Add(AdminBffWarning.CallFailed("Vessel.Detail", "Could not retrieve vessel detail."));

        if (docsTask.IsCompletedSuccessfully)
            response.Documents = docsTask.Result.Body;
        else
            response.Warnings.Add(AdminBffWarning.CallFailed("Vessel.Documents", "Could not retrieve vessel documents."));

        if (ownersTask.IsCompletedSuccessfully)
            response.Owners = ownersTask.Result.Body;
        else
            response.Warnings.Add(AdminBffWarning.CallFailed("Vessel.Owners", "Could not retrieve vessel owners."));

        return response;
    }
}
