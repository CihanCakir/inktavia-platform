using Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.CQRS.Handler;
using Aizen.Bff.AdminPanel.Application.Common.Services;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Query;

[DocumentationInfo("Get admin service request list query handler", "Fetches the paged admin service request list with optional status and vessel filters.")]
public sealed class GetAdminServiceRequestListQueryHandler
    : AizenQueryHandler<GetAdminServiceRequestListQuery, AdminServiceRequestListResponse>
{
    private readonly IServiceRequestAdminBffRemoteCall _serviceRequest;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;

    public GetAdminServiceRequestListQueryHandler(IServiceRequestAdminBffRemoteCall serviceRequest,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider)
    {
        _serviceRequest = serviceRequest;
        _serviceTokenProvider = serviceTokenProvider;
    }

    public override async Task<AdminServiceRequestListResponse?> Handle(
        GetAdminServiceRequestListQuery request, CancellationToken cancellationToken)
    {
        var response = new AdminServiceRequestListResponse();

        try
        {
            var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(cancellationToken);
        var authHeader = $"Bearer {serviceToken}";

        var result = await _serviceRequest.GetAdminServiceRequestList(
                authHeader, request.UserToken, request.Status, request.VesselId, request.PageIndex, request.PageSize);
            response.ServiceRequests = result.Body;
        }
        catch
        {
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("ServiceRequest"));
        }

        return response;
    }
}
