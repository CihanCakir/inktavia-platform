using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Offer;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Offer;

namespace Aizen.Modules.ServiceRequest.Application.Command.Offer;

[DocumentationInfo("Update offer command", "Provider updates an existing submitted offer.")]
public sealed class UpdateServiceRequestOfferCommand : AizenCommand<UpdateServiceRequestOfferResponse>
{
    public long ServiceRequestId { get; }
    public long OfferId { get; }
    public UpdateServiceRequestOfferRequest Request { get; }

    public UpdateServiceRequestOfferCommand(long serviceRequestId, long offerId, UpdateServiceRequestOfferRequest request)
    {
        ServiceRequestId = serviceRequestId;
        OfferId = offerId;
        Request = request;
    }
}
