using Aizen.Bff.Marine.Web.Application.Contracts.Pricing;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Web.Application.Pricing.Query.GetWebPricing;

/// <summary>
/// GET /api/v1/web/pricing — published subscription-plan terms for the website (W4, PARTIAL). Aggregates the
/// Payment module's public provider + participant plan lists into one web pricing projection.
/// </summary>
public sealed class GetWebPricingQuery : AizenQuery<WebPricingDto>
{
}
