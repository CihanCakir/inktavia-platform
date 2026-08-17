using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;

namespace Aizen.Modules.Payment.Application.Queries.GetPublicPricingTerms;

/// <summary>
/// GET /api/v1/payment/public/pricing-terms — the published, web-safe pricing terms (M1): the Global STANDARD
/// commission default + the Global platform-fee headline. No context, no internal economics.
/// </summary>
public sealed class GetPublicPricingTermsQuery : AizenQuery<PublicPricingTermsDto>
{
}
