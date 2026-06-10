using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Completion;
using Aizen.Bff.AdminPanel.Application.Common.Services;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Command;

[DocumentationInfo("Reject completion command handler", "Rejects a service request completion via the ServiceRequest module.")]
public sealed class RejectCompletionCommandHandler
    : AizenCommandHandler<RejectCompletionCommand, RejectServiceRequestCompletionResponse>
{
    private readonly IServiceRequestAdminBffRemoteCall _serviceRequest;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;

    public RejectCompletionCommandHandler(IServiceRequestAdminBffRemoteCall serviceRequest,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider)
    {
        _serviceRequest = serviceRequest;
        _serviceTokenProvider = serviceTokenProvider;
    }

    public override async Task<RejectServiceRequestCompletionResponse?> Handle(
        RejectCompletionCommand request, CancellationToken cancellationToken)
    {
        var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(cancellationToken);
        var authHeader = $"Bearer {serviceToken}";

        var result = await _serviceRequest.RejectCompletion(request.ServiceRequestId, request.Payload, authHeader, request.UserToken);
        return result.Body;
    }
}
