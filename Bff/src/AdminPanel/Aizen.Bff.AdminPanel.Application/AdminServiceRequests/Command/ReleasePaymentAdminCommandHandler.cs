using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Services;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Command;

[DocumentationInfo("Release payment admin command handler", "Releases payment for a completed service request via the ServiceRequest module.")]
public sealed class ReleasePaymentAdminCommandHandler
    : AizenCommandHandler<ReleasePaymentAdminCommand, ReleasePaymentResponse>
{
    private readonly IServiceRequestAdminBffRemoteCall _serviceRequest;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;

    public ReleasePaymentAdminCommandHandler(IServiceRequestAdminBffRemoteCall serviceRequest,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider)
    {
        _serviceRequest = serviceRequest;
        _serviceTokenProvider = serviceTokenProvider;
    }

    public override async Task<ReleasePaymentResponse?> Handle(
        ReleasePaymentAdminCommand request, CancellationToken cancellationToken)
    {
        var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(cancellationToken);
        var authHeader = $"Bearer {serviceToken}";

        var result = await _serviceRequest.ReleaseServiceRequestPayment(
            request.ServiceRequestId, authHeader, request.UserToken);
        return result.Body;
    }
}
