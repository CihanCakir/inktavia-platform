using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Offer;

namespace Aizen.Bff.MarineProvider.Application.Offers;

public sealed class CreateOfferBffCommand : AizenCommand<CreateOfferBffResponse>
{
    public long ServiceRequestId { get; init; }
    public CreateServiceRequestOfferRequest Body { get; init; } = default!;
}

public sealed class CreateOfferBffResponse
{
    public bool Success { get; set; }
    public long? OfferId { get; set; }
    public string? Message { get; set; }
}
