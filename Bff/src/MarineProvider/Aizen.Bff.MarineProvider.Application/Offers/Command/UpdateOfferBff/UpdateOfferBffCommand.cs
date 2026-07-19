using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Offer;

namespace Aizen.Bff.MarineProvider.Application.Offers;

public sealed class UpdateOfferBffCommand : AizenCommand<UpdateOfferBffResponse>
{
    public long ServiceRequestId { get; init; }
    public long OfferId { get; init; }
    public UpdateServiceRequestOfferRequest Body { get; init; } = default!;
}

public sealed class UpdateOfferBffResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}
