using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Services;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Offer;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Command;

[DocumentationInfo("Accept service request offer admin command handler", "Accepts a provider offer on a service request via the ServiceRequest module.")]
public sealed class AcceptServiceRequestOfferAdminCommandHandler
    : AizenCommandHandler<AcceptServiceRequestOfferAdminCommand, AcceptServiceRequestOfferResponse>
{
    private readonly IServiceRequestAdminBffRemoteCall _serviceRequest;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;

    public AcceptServiceRequestOfferAdminCommandHandler(IServiceRequestAdminBffRemoteCall serviceRequest,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider)
    {
        _serviceRequest = serviceRequest;
        _serviceTokenProvider = serviceTokenProvider;
    }

    public override async Task<AcceptServiceRequestOfferResponse?> Handle(
        AcceptServiceRequestOfferAdminCommand request, CancellationToken cancellationToken)
    {
        var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(cancellationToken);
        var authHeader = $"Bearer {serviceToken}";

        var result = await _serviceRequest.AcceptServiceRequestOffer(
            request.ServiceRequestId, request.OfferId, authHeader, request.UserToken);
        return result.Body;
    }
}
