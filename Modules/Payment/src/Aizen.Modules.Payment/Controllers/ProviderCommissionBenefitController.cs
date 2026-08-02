using Aizen.Modules.Payment.Application.Commands.CommissionBenefit;
using Aizen.Modules.Payment.Application.Commands.ReactivateProviderCommissionBenefitRule;
using Aizen.Modules.Payment.Application.Queries.GetProviderCommissionBenefitEntitlementById;
using Aizen.Modules.Payment.Application.Queries.GetProviderCommissionBenefitEntitlementsList;
using Aizen.Modules.Payment.Application.Queries.GetProviderCommissionBenefitRuleById;
using Aizen.Modules.Payment.Application.Queries.GetProviderCommissionBenefitRulesList;
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
    [HttpGet("rules")]
    public async Task<IActionResult> List(
        [FromQuery] long?    providerProfileId,
        [FromQuery] long?    providerPlanId,
        [FromQuery] string?  categoryCode,
        [FromQuery] string?  currencyCode,
        [FromQuery] bool?    stackable,
        [FromQuery] bool?    isActive,
        CancellationToken ct = default)
        => Ok(await _sender.Send(new GetProviderCommissionBenefitRulesListQuery
        {
            ProviderProfileId = providerProfileId,
            ProviderPlanId    = providerPlanId,
            CategoryCode      = categoryCode,
            CurrencyCode      = currencyCode,
            Stackable         = stackable,
            IsActive          = isActive,
        }, ct));

    [HttpGet("rules/{id:long}")]
    public async Task<IActionResult> Detail(long id, CancellationToken ct = default)
        => Ok(await _sender.Send(new GetProviderCommissionBenefitRuleByIdQuery { Id = id }, ct));

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

    [HttpPost("rules/{id:long}/reactivate")]
    public async Task<IActionResult> Reactivate(long id, CancellationToken ct = default)
        => Ok(await _sender.Send(new ReactivateProviderCommissionBenefitRuleCommand { Id = id }, ct));

    // ── Entitlements ─────────────────────────────────────────────────────────────
    [HttpGet("entitlements")]
    public async Task<IActionResult> ListEntitlements(
        [FromQuery] long?    providerProfileId,
        [FromQuery] long?    benefitRuleId,
        [FromQuery] bool?    isActive,
        CancellationToken ct = default)
        => Ok(await _sender.Send(new GetProviderCommissionBenefitEntitlementsListQuery
        {
            ProviderProfileId = providerProfileId,
            BenefitRuleId     = benefitRuleId,
            IsActive          = isActive,
        }, ct));

    [HttpGet("entitlements/{id:long}")]
    public async Task<IActionResult> EntitlementDetail(long id, CancellationToken ct = default)
        => Ok(await _sender.Send(new GetProviderCommissionBenefitEntitlementByIdQuery { Id = id }, ct));

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
