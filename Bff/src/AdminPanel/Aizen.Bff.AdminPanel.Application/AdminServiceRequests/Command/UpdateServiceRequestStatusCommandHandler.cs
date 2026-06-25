using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Services;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Command;

[DocumentationInfo("Update service request status command handler", "Updates the status of a service request via the ServiceRequest module.")]
public sealed class UpdateServiceRequestStatusCommandHandler
    : AizenCommandHandler<UpdateServiceRequestStatusCommand, UpdateServiceRequestStatusResponse>
{
    private readonly IServiceRequestAdminBffRemoteCall _serviceRequest;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;

    public UpdateServiceRequestStatusCommandHandler(IServiceRequestAdminBffRemoteCall serviceRequest,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider)
    {
        _serviceRequest = serviceRequest;
        _serviceTokenProvider = serviceTokenProvider;
    }

    public override async Task<UpdateServiceRequestStatusResponse?> Handle(
        UpdateServiceRequestStatusCommand request, CancellationToken cancellationToken)
    {
        var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(cancellationToken);
        var authHeader = $"Bearer {serviceToken}";

        var result = await _serviceRequest.UpdateAdminServiceRequestStatus(
            request.ServiceRequestId, request.Payload, authHeader, request.UserToken);
        return result.Body;
    }
}
