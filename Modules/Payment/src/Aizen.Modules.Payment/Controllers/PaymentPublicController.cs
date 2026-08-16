using Aizen.Modules.Payment.Application.Queries.GetPublicPricingTerms;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.Payment.Controllers;

/// <summary>
/// Public, anonymous Payment reads for the marketing website (M1). Mirrors the existing anonymous plan GETs
/// (`participant-plans` / `provider-plans`): in-cluster only (the NetworkPolicy admits the BFFs), no Keycloak role.
/// Read-only projection of the published pricing defaults — no economics-calculation path, no admin surface.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/payment/public")]
public sealed class PaymentPublicController : ControllerBase
{
    private readonly ISender _sender;
    public PaymentPublicController(ISender sender) => _sender = sender;

    /// <summary>
    /// Returns the published, web-safe pricing terms: the Global STANDARD commission default + the Global
    /// platform-fee headline (no VAT, no internal economics). Never authenticated.
    /// </summary>
    [HttpGet("pricing-terms")]
    public async Task<IActionResult> GetPricingTerms(CancellationToken ct = default)
        => Ok(await _sender.Send(new GetPublicPricingTermsQuery(), ct));
}
