using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Services;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Command;

[DocumentationInfo("Complete service request admin command handler", "Marks a service request as completed via the ServiceRequest module.")]
public sealed class CompleteServiceRequestAdminCommandHandler
    : AizenCommandHandler<CompleteServiceRequestAdminCommand, CompleteServiceRequestResponse>
{
    private readonly IServiceRequestAdminBffRemoteCall _serviceRequest;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;

    public CompleteServiceRequestAdminCommandHandler(IServiceRequestAdminBffRemoteCall serviceRequest,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider)
    {
        _serviceRequest = serviceRequest;
        _serviceTokenProvider = serviceTokenProvider;
    }

    public override async Task<CompleteServiceRequestResponse?> Handle(
        CompleteServiceRequestAdminCommand request, CancellationToken cancellationToken)
    {
        var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(cancellationToken);
        var authHeader = $"Bearer {serviceToken}";

        var result = await _serviceRequest.CompleteServiceRequest(
            request.ServiceRequestId, authHeader, request.UserToken);
        return result.Body;
    }
}
