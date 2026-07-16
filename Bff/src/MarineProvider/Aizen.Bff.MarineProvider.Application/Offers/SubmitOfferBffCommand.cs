using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Offer;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Offer;

namespace Aizen.Bff.MarineProvider.Application.Offers;

public sealed class SubmitOfferBffCommand : AizenCommand<SubmitOfferResponse>
{
    public long ServiceRequestId { get; init; }
    public long OfferId { get; init; }
    public SubmitOfferRequest Body { get; init; } = default!;
}
