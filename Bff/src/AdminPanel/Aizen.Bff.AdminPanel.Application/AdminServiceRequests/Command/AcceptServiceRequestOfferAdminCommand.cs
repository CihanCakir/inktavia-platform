using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Offer;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Command;

public sealed class AcceptServiceRequestOfferAdminCommand : AizenCommand<AcceptServiceRequestOfferResponse>
{
    public long ServiceRequestId { get; }
    public long OfferId { get; }

    public AcceptServiceRequestOfferAdminCommand(long serviceRequestId, long offerId)
    {
        ServiceRequestId = serviceRequestId;
        OfferId = offerId;
    }
}
