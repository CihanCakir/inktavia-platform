using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Model;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Offer;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Offer;

namespace Aizen.Modules.ServiceRequest.Application.Command.Offer;

[DocumentationInfo("Reject offer command", "Owner rejects a service request offer.")]
public sealed class RejectServiceRequestOfferCommand : AizenCommand<RejectServiceRequestOfferResponse>
{
    public long ServiceRequestId { get; }
    public RejectServiceRequestOfferRequest Request { get; }
    public RejectServiceRequestOfferCommand(long serviceRequestId, RejectServiceRequestOfferRequest request)
    {
        ServiceRequestId = serviceRequestId; Request = request;
    }
}
