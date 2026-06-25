using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Model;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Offer;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Offer;

namespace Aizen.Modules.ServiceRequest.Application.Command.Offer;

[DocumentationInfo("Withdraw offer command", "Provider withdraws a submitted offer.")]
public sealed class WithdrawServiceRequestOfferCommand : AizenCommand<WithdrawServiceRequestOfferResponse>
{
    public long ServiceRequestId { get; }
    public long OfferId { get; }
    public WithdrawServiceRequestOfferRequest Request { get; }
    public WithdrawServiceRequestOfferCommand(long serviceRequestId, long offerId, WithdrawServiceRequestOfferRequest request)
    {
        ServiceRequestId = serviceRequestId; OfferId = offerId; Request = request;
    }
}
