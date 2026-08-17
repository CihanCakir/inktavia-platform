using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Offer;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Offer;

namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Command;

public sealed class RejectServiceRequestOfferBffCommand : AizenCommand<RejectServiceRequestOfferResponse>
{
    public long ServiceRequestId { get; }
    public long OfferId { get; }
    public RejectServiceRequestOfferRequest Payload { get; }

    public RejectServiceRequestOfferBffCommand(long serviceRequestId, long offerId, RejectServiceRequestOfferRequest payload)
    {
        ServiceRequestId = serviceRequestId;
        OfferId = offerId;
        Payload = payload;
    }
}
