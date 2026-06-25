using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Services;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ServiceRequest.Abstraction.Response.WorkLog;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Command;

[DocumentationInfo("Update work phase admin command handler", "Updates a work phase's progress and status via the ServiceRequest module.")]
public sealed class UpdateWorkPhaseAdminCommandHandler
    : AizenCommandHandler<UpdateWorkPhaseAdminCommand, UpdateWorkPhaseResponse>
{
    private readonly IServiceRequestAdminBffRemoteCall _serviceRequest;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;

    public UpdateWorkPhaseAdminCommandHandler(IServiceRequestAdminBffRemoteCall serviceRequest,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider)
    {
        _serviceRequest = serviceRequest;
        _serviceTokenProvider = serviceTokenProvider;
    }

    public override async Task<UpdateWorkPhaseResponse?> Handle(
        UpdateWorkPhaseAdminCommand request, CancellationToken cancellationToken)
    {
        var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(cancellationToken);
        var authHeader = $"Bearer {serviceToken}";

        var result = await _serviceRequest.UpdateAdminWorkPhase(
            request.ServiceRequestId, request.PhaseNumber, request.Payload, authHeader, request.UserToken);
        return result.Body;
    }
}
