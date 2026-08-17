using Aizen.Bff.Marine.Web.Application.Contracts.Pricing;
using Aizen.Bff.Marine.Web.Application.Pricing.Query.GetWebPricing;
using Aizen.Bff.Marine.Web.Filters;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Aizen.Bff.Marine.Web.Controllers.V1;

/// <summary>
/// Public pricing for the website (W4 priority 5, PARTIAL) — anonymous, cached, trusted-caller aware. Thin:
/// dispatches a query. Carries published subscription-plan terms only; commission model, customer platform fee and
/// the VAT flag are admin-only in Payment and are reported BLOCKED in <c>docs/MARINE_WEB_BLOCKED.md</c>.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/web/pricing")]
[Tags("Web - Pricing")]
public sealed class PricingController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public PricingController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
        => _cqrs = cqrs;

    [HttpGet]
    [EnableRateLimiting("public-read-ip")]
    [WebCache(WebCacheAttribute.PricingMaxAge, WebCacheAttribute.PricingSwr)]
    public async Task<AizenApiResponse<WebPricingDto?>> Pricing(CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new GetWebPricingQuery(), ct);
        return SetResponse(result);
    }
}
