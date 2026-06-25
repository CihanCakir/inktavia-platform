using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Services;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Offer;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Command;

[DocumentationInfo("Reject service request offer admin command handler", "Rejects a provider offer on a service request via the ServiceRequest module.")]
public sealed class RejectServiceRequestOfferAdminCommandHandler
    : AizenCommandHandler<RejectServiceRequestOfferAdminCommand, RejectServiceRequestOfferResponse>
{
    private readonly IServiceRequestAdminBffRemoteCall _serviceRequest;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;

    public RejectServiceRequestOfferAdminCommandHandler(IServiceRequestAdminBffRemoteCall serviceRequest,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider)
    {
        _serviceRequest = serviceRequest;
        _serviceTokenProvider = serviceTokenProvider;
    }

    public override async Task<RejectServiceRequestOfferResponse?> Handle(
        RejectServiceRequestOfferAdminCommand request, CancellationToken cancellationToken)
    {
        var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(cancellationToken);
        var authHeader = $"Bearer {serviceToken}";

        var result = await _serviceRequest.RejectServiceRequestOffer(
            request.ServiceRequestId, request.OfferId, request.Payload, authHeader, request.UserToken);
        return result.Body;
    }
}
