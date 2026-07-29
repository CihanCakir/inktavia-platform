using Aizen.Modules.Payment.Application.Commands.CommissionBenefit;
using Aizen.Modules.Payment.Application.Queries.ResolveEffectiveCommission;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.Payment.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/payment/commission-benefits")]
public sealed class ProviderCommissionBenefitController : ControllerBase
{
    private readonly ISender _sender;
    public ProviderCommissionBenefitController(ISender sender) => _sender = sender;

    /// <summary>What-if: base (BE-P2) → effective commission rate + benefit cost (BE-P7 two-stage).</summary>
    [HttpGet("resolve")]
    public async Task<IActionResult> Resolve(
        [FromQuery] long     providerProfileId,
        [FromQuery] long?    providerPlanId,
        [FromQuery] string?  categoryCode,
        [FromQuery] decimal  serviceAmount,
        [FromQuery] string   currencyCode = "TRY",
        [FromQuery] decimal? eligibleGmvRemaining = null,
        [FromQuery] decimal  planFloorRate = 0m,
        CancellationToken ct = default)
        => Ok(await _sender.Send(new ResolveEffectiveCommissionQuery
        {
            ProviderProfileId    = providerProfileId,
            ProviderPlanId       = providerPlanId,
            CategoryCode         = categoryCode,
            ServiceAmount        = serviceAmount,
            CurrencyCode         = currencyCode,
            EligibleGmvRemaining = eligibleGmvRemaining,
            PlanFloorRate        = planFloorRate,
        }, ct));

    // ── Rule CRUD ────────────────────────────────────────────────────────────────
    [HttpPost("rules")]
    public async Task<IActionResult> Create([FromBody] CreateProviderCommissionBenefitRuleCommand c, CancellationToken ct = default)
        => Ok(await _sender.Send(c, ct));

    [HttpPut("rules/{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateProviderCommissionBenefitRuleCommand c, CancellationToken ct = default)
    {
        if (id != c.Id) return BadRequest("Route id and body id must match.");
        return Ok(await _sender.Send(c, ct));
    }

    [HttpPost("rules/{id:long}/deactivate")]
    public async Task<IActionResult> Deactivate(long id, CancellationToken ct = default)
        => Ok(await _sender.Send(new DeactivateProviderCommissionBenefitRuleCommand { Id = id }, ct));

    // ── Entitlements ─────────────────────────────────────────────────────────────
    [HttpPost("entitlements")]
    public async Task<IActionResult> Grant([FromBody] GrantProviderCommissionBenefitEntitlementCommand c, CancellationToken ct = default)
        => Ok(await _sender.Send(c, ct));

    [HttpPost("entitlements/{id:long}/revoke")]
    public async Task<IActionResult> Revoke(long id, CancellationToken ct = default)
        => Ok(await _sender.Send(new RevokeProviderCommissionBenefitEntitlementCommand { Id = id }, ct));

    [HttpPost("reserve")]
    public async Task<IActionResult> Reserve([FromBody] ReserveCommissionBenefitCommand c, CancellationToken ct = default)
        => Ok(await _sender.Send(c, ct));

    [HttpPost("usages/{usageId:long}/consume")]
    public async Task<IActionResult> Consume(long usageId, CancellationToken ct = default)
        => Ok(await _sender.Send(new ConsumeCommissionBenefitCommand { UsageId = usageId }, ct));

    [HttpPost("usages/{usageId:long}/release")]
    public async Task<IActionResult> Release(long usageId, CancellationToken ct = default)
        => Ok(await _sender.Send(new ReleaseCommissionBenefitCommand { UsageId = usageId }, ct));
}
