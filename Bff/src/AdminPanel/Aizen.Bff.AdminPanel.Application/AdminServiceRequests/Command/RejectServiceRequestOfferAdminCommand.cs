using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Offer;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Offer;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Command;

public sealed class RejectServiceRequestOfferAdminCommand : AizenCommand<RejectServiceRequestOfferResponse>
{
    public long ServiceRequestId { get; }
    public long OfferId { get; }
    public RejectServiceRequestOfferRequest Payload { get; }
    public string UserToken { get; }

    public RejectServiceRequestOfferAdminCommand(long serviceRequestId, long offerId, RejectServiceRequestOfferRequest payload, string userToken)
    {
        ServiceRequestId = serviceRequestId;
        OfferId = offerId;
        Payload = payload;
        UserToken = userToken;
    }
}
