using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Offer;

namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Command;

public sealed class AcceptServiceRequestOfferBffCommand : AizenCommand<AcceptServiceRequestOfferResponse>
{
    public long ServiceRequestId { get; }
    public long OfferId { get; }

    public AcceptServiceRequestOfferBffCommand(long serviceRequestId, long offerId)
    {
        ServiceRequestId = serviceRequestId;
        OfferId = offerId;
    }
}
