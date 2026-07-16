using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Offer;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Offer;

namespace Aizen.Modules.ServiceRequest.Application.Command.Offer.PreviewOffer;

public sealed class PreviewOfferCommand : AizenCommand<PreviewOfferResponse>
{
    public long ServiceRequestId { get; }
    public SaveOfferDraftRequest Request { get; }

    public PreviewOfferCommand(long serviceRequestId, SaveOfferDraftRequest request)
    {
        ServiceRequestId = serviceRequestId;
        Request = request;
    }
}
