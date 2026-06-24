using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Services;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Command;

[DocumentationInfo("Assign service request provider command handler", "Assigns a provider to a service request via the ServiceRequest module.")]
public sealed class AssignServiceRequestProviderCommandHandler
    : AizenCommandHandler<AssignServiceRequestProviderCommand, AssignProviderResponse>
{
    private readonly IServiceRequestAdminBffRemoteCall _serviceRequest;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;

    public AssignServiceRequestProviderCommandHandler(IServiceRequestAdminBffRemoteCall serviceRequest,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider)
    {
        _serviceRequest = serviceRequest;
        _serviceTokenProvider = serviceTokenProvider;
    }

    public override async Task<AssignProviderResponse?> Handle(
        AssignServiceRequestProviderCommand request, CancellationToken cancellationToken)
    {
        var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(cancellationToken);
        var authHeader = $"Bearer {serviceToken}";

        var result = await _serviceRequest.AssignServiceRequestProvider(
            request.ServiceRequestId, request.Payload, authHeader, request.UserToken);
        return result.Body;
    }
}
