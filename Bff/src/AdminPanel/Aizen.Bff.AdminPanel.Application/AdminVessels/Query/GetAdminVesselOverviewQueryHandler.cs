using Aizen.Bff.AdminPanel.Application.AdminVessels.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.CQRS.Handler;
using Aizen.Bff.AdminPanel.Application.Common.Services;

namespace Aizen.Bff.AdminPanel.Application.AdminVessels.Query;

[DocumentationInfo("Get admin vessel overview query handler", "Fetches the paged admin vessel list with optional search and archive filter.")]
public sealed class GetAdminVesselOverviewQueryHandler
    : AizenQueryHandler<GetAdminVesselOverviewQuery, AdminVesselOverviewResponse>
{
    private readonly IVesselAdminBffRemoteCall _vessel;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;

    public GetAdminVesselOverviewQueryHandler(IVesselAdminBffRemoteCall vessel,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider)
    {
        _vessel = vessel;
        _serviceTokenProvider = serviceTokenProvider;
    }

    public override async Task<AdminVesselOverviewResponse?> Handle(
        GetAdminVesselOverviewQuery request, CancellationToken cancellationToken)
    {
        var response = new AdminVesselOverviewResponse();

        try
        {
            var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(cancellationToken);
        var authHeader = $"Bearer {serviceToken}";

        var result = await _vessel.GetAdminVesselList(
                authHeader,
                request.UserToken,
                request.PageIndex,
                request.PageSize,
                request.SearchTerm,
                request.IsArchived);

            response.Vessels = result.Body;
        }
        catch
        {
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("Vessel"));
        }

        return response;
    }
}
