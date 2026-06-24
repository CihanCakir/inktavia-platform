using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Services;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Command;

[DocumentationInfo("Dispute service request admin command handler", "Opens a dispute on a service request via the ServiceRequest module.")]
public sealed class DisputeServiceRequestAdminCommandHandler
    : AizenCommandHandler<DisputeServiceRequestAdminCommand, DisputeServiceRequestResponse>
{
    private readonly IServiceRequestAdminBffRemoteCall _serviceRequest;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;

    public DisputeServiceRequestAdminCommandHandler(IServiceRequestAdminBffRemoteCall serviceRequest,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider)
    {
        _serviceRequest = serviceRequest;
        _serviceTokenProvider = serviceTokenProvider;
    }

    public override async Task<DisputeServiceRequestResponse?> Handle(
        DisputeServiceRequestAdminCommand request, CancellationToken cancellationToken)
    {
        var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(cancellationToken);
        var authHeader = $"Bearer {serviceToken}";

        var result = await _serviceRequest.DisputeServiceRequest(
            request.ServiceRequestId, request.Payload, authHeader, request.UserToken);
        return result.Body;
    }
}
