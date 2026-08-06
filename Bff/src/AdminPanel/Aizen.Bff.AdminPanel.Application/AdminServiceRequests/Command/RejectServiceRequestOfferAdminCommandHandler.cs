using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Offer;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Command;

[DocumentationInfo("Reject service request offer admin command handler", "Rejects a provider offer on a service request via the ServiceRequest module.")]
public sealed class RejectServiceRequestOfferAdminCommandHandler
    : AizenCommandHandler<RejectServiceRequestOfferAdminCommand, RejectServiceRequestOfferResponse>
{
    private readonly IServiceRequestRemoteCall _serviceRequest;

    public RejectServiceRequestOfferAdminCommandHandler(IServiceRequestRemoteCall serviceRequest)
    {
        _serviceRequest = serviceRequest;
    }

    public override async Task<RejectServiceRequestOfferResponse?> Handle(
        RejectServiceRequestOfferAdminCommand request, CancellationToken cancellationToken)
    {

        var result = await _serviceRequest.RejectServiceRequestOffer(
            request.ServiceRequestId, request.OfferId, request.Payload);
        return result.Body;
    }
}
