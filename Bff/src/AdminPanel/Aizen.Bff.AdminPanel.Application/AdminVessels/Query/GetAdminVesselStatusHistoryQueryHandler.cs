using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Services;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Vessel.Abstraction.Response.Status;

namespace Aizen.Bff.AdminPanel.Application.AdminVessels.Query;

[DocumentationInfo("Get admin vessel status history query handler", "Fetches vessel status change history from the Vessel module.")]
public sealed class GetAdminVesselStatusHistoryQueryHandler
    : AizenQueryHandler<GetAdminVesselStatusHistoryQuery, GetVesselStatusHistoryResponse>
{
    private readonly IVesselAdminBffRemoteCall _vessel;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;

    public GetAdminVesselStatusHistoryQueryHandler(
        IVesselAdminBffRemoteCall vessel,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider)
    {
        _vessel = vessel;
        _serviceTokenProvider = serviceTokenProvider;
    }

    public override async Task<GetVesselStatusHistoryResponse?> Handle(
        GetAdminVesselStatusHistoryQuery request, CancellationToken cancellationToken)
    {
        var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(cancellationToken);
        var authHeader = $"Bearer {serviceToken}";

        var result = await _vessel.GetVesselStatusHistory(
            request.VesselId, authHeader, request.UserToken,
            request.PageIndex, request.PageSize);

        return result.Body;
    }
}
