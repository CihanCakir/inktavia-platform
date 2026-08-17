using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Offer;

namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Command;

[DocumentationInfo("Accept service request offer admin command handler", "Accepts a provider offer on a service request via the ServiceRequest module.")]
public sealed class AcceptServiceRequestOfferBffCommandHandler
    : AizenCommandHandler<AcceptServiceRequestOfferBffCommand, AcceptServiceRequestOfferResponse>
{
    private readonly IServiceRequestRemoteCall _serviceRequest;

    public AcceptServiceRequestOfferBffCommandHandler(IServiceRequestRemoteCall serviceRequest)
    {
        _serviceRequest = serviceRequest;
    }

    public override async Task<AcceptServiceRequestOfferResponse?> Handle(
        AcceptServiceRequestOfferBffCommand request, CancellationToken cancellationToken)
    {

        var result = await _serviceRequest.AcceptServiceRequestOffer(
            request.ServiceRequestId, request.OfferId);
        return result.Body;
    }
}
