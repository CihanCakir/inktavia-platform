using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Dispute;
using Aizen.Bff.AdminPanel.Application.Common.Services;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Command;

[DocumentationInfo("Resolve dispute command handler", "Resolves a service request dispute via the ServiceRequest module.")]
public sealed class ResolveDisputeCommandHandler
    : AizenCommandHandler<ResolveDisputeCommand, ResolveServiceRequestDisputeResponse>
{
    private readonly IServiceRequestAdminBffRemoteCall _serviceRequest;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;

    public ResolveDisputeCommandHandler(IServiceRequestAdminBffRemoteCall serviceRequest,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider)
    {
        _serviceRequest = serviceRequest;
        _serviceTokenProvider = serviceTokenProvider;
    }

    public override async Task<ResolveServiceRequestDisputeResponse?> Handle(
        ResolveDisputeCommand request, CancellationToken cancellationToken)
    {
        var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(cancellationToken);
        var authHeader = $"Bearer {serviceToken}";

        var result = await _serviceRequest.ResolveDispute(
            request.ServiceRequestId, request.DisputeId, request.Payload, authHeader, request.UserToken);
        return result.Body;
    }
}
