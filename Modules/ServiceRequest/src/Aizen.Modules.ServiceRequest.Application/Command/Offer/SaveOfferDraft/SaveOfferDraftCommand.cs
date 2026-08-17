using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Offer;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Offer;

namespace Aizen.Modules.ServiceRequest.Application.Command.Offer.SaveOfferDraft;

public sealed class SaveOfferDraftCommand : AizenCommand<SaveOfferDraftResponse>
{
    public long ServiceRequestId { get; }
    public SaveOfferDraftRequest Request { get; }

    public SaveOfferDraftCommand(long serviceRequestId, SaveOfferDraftRequest request)
    {
        ServiceRequestId = serviceRequestId;
        Request = request;
    }
}
