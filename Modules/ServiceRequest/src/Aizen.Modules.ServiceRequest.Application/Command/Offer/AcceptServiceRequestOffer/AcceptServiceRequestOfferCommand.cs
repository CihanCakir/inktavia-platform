using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Offer;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Offer;

namespace Aizen.Modules.ServiceRequest.Application.Command.Offer;

[DocumentationInfo("Accept offer command", "Owner accepts a service request offer.")]
public sealed class AcceptServiceRequestOfferCommand : AizenCommand<AcceptServiceRequestOfferResponse>
{
    public long ServiceRequestId { get; }
    public AcceptServiceRequestOfferRequest Request { get; }
    public AcceptServiceRequestOfferCommand(long serviceRequestId, AcceptServiceRequestOfferRequest request)
    {
        ServiceRequestId = serviceRequestId; Request = request;
    }
}
