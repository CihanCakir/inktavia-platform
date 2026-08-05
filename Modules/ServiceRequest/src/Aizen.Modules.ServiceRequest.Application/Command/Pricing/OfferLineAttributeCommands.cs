using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Dto.Pricing;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Pricing;

namespace Aizen.Modules.ServiceRequest.Application.Command.Pricing;

/// <summary>S2b — provider sets (full-replace) the pricing attribute values on one offer line.</summary>
public sealed class SetOfferLineAttributesCommand : AizenCommand<List<PricingAttributeValueDto>>
{
    public long OfferId { get; }
    public long OfferItemId { get; }
    public SetOfferLineAttributesRequest Request { get; }
    public SetOfferLineAttributesCommand(long offerId, long offerItemId, SetOfferLineAttributesRequest request)
    { OfferId = offerId; OfferItemId = offerItemId; Request = request; }
}

/// <summary>S2b — the current attribute values on one offer line.</summary>
public sealed class GetOfferLineAttributesQuery : AizenQuery<List<PricingAttributeValueDto>>
{
    public long OfferItemId { get; }
    public GetOfferLineAttributesQuery(long offerItemId) => OfferItemId = offerItemId;
}

/// <summary>S2b/S2c — the pricing attributes applicable to an SR's category, with resolved Lookup options (FE picker).</summary>
public sealed class GetApplicablePricingAttributesQuery : AizenQuery<List<ApplicablePricingAttributeDto>>
{
    public long ServiceRequestId { get; }
    public GetApplicablePricingAttributesQuery(long serviceRequestId) => ServiceRequestId = serviceRequestId;
}
