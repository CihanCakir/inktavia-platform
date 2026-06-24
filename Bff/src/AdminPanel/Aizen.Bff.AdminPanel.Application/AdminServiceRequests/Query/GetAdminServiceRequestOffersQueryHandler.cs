using Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Services;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Query;

[DocumentationInfo("Get admin service request offers query handler", "Fetches provider offers and agreement timeline for a service request.")]
public sealed class GetAdminServiceRequestOffersQueryHandler
    : AizenQueryHandler<GetAdminServiceRequestOffersQuery, AdminServiceRequestOffersResponse>
{
    private readonly IServiceRequestAdminBffRemoteCall _serviceRequest;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;

    public GetAdminServiceRequestOffersQueryHandler(IServiceRequestAdminBffRemoteCall serviceRequest,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider)
    {
        _serviceRequest = serviceRequest;
        _serviceTokenProvider = serviceTokenProvider;
    }

    public override async Task<AdminServiceRequestOffersResponse?> Handle(
        GetAdminServiceRequestOffersQuery request, CancellationToken cancellationToken)
    {
        var response = new AdminServiceRequestOffersResponse();

        try
        {
            var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(cancellationToken);
            var authHeader = $"Bearer {serviceToken}";

            var result = await _serviceRequest.GetAdminServiceRequestOffers(
                request.ServiceRequestId, authHeader, request.UserToken);
            response.Offers = result.Body;
        }
        catch
        {
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("ServiceRequest"));
        }

        return response;
    }
}
