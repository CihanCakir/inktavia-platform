using Aizen.Modules.Payment.Application.Queries.ResolveCommissionRate;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.Payment.Controllers;

[ApiController]
[Authorize(Roles = "payment.admin")]
[Route("api/v1/payment/commission")]
public sealed class CommissionRuleController : ControllerBase
{
    private readonly ISender _sender;
    public CommissionRuleController(ISender sender) => _sender = sender;

    /// <summary>
    /// Resolves and returns the effective commission rate for given context.
    /// Useful for admin preview and SR payment calculation.
    /// </summary>
    [HttpGet("resolve")]
    public async Task<IActionResult> ResolveRate(
        [FromQuery] long?   providerProfileId,
        [FromQuery] long?   providerPlanId,
        [FromQuery] string? categoryCode,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new ResolveCommissionRateQuery
        {
            ProviderProfileId = providerProfileId,
            ProviderPlanId    = providerPlanId,
            CategoryCode      = categoryCode,
        }, ct);
        return Ok(result);
    }
}
