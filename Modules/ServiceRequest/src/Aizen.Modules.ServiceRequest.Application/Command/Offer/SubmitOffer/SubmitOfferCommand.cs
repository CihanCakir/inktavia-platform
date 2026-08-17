using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Offer;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Offer;

namespace Aizen.Modules.ServiceRequest.Application.Command.Offer.SubmitOffer;

public sealed class SubmitOfferCommand : AizenCommand<SubmitOfferResponse>
{
    public long ServiceRequestId { get; }
    public long OfferId { get; }
    public SubmitOfferRequest Request { get; }

    public SubmitOfferCommand(long serviceRequestId, long offerId, SubmitOfferRequest request)
    {
        ServiceRequestId = serviceRequestId;
        OfferId = offerId;
        Request = request;
    }
}
