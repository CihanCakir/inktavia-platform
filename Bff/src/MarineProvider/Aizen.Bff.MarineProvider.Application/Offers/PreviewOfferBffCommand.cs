using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Offer;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Offer;

namespace Aizen.Bff.MarineProvider.Application.Offers;

public sealed class PreviewOfferBffCommand : AizenCommand<PreviewOfferResponse>
{
    public long ServiceRequestId { get; init; }
    public SaveOfferDraftRequest Body { get; init; } = default!;
}
