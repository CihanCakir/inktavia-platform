using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Dto.Travel;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Travel;

namespace Aizen.Modules.ServiceRequest.Application.Command.Travel;

/// <summary>BE-S4a — provider sets (upsert) the structured travel-pricing detail on one Travel offer line.</summary>
public sealed class SetOfferLineTravelPricingCommand : AizenCommand<TravelPricingDetailDto>
{
    public long OfferId { get; }
    public long OfferItemId { get; }
    public SetOfferLineTravelPricingRequest Request { get; }
    public SetOfferLineTravelPricingCommand(long offerId, long offerItemId, SetOfferLineTravelPricingRequest request)
    { OfferId = offerId; OfferItemId = offerItemId; Request = request; }
}

/// <summary>BE-S4a — the current travel-pricing detail on one offer line (null when none is set).</summary>
public sealed class GetOfferLineTravelPricingQuery : AizenQuery<TravelPricingDetailDto>
{
    public long OfferItemId { get; }
    public GetOfferLineTravelPricingQuery(long offerItemId) => OfferItemId = offerItemId;
}
