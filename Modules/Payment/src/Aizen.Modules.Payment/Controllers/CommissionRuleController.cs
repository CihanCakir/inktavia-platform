using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Application.Commands.CreateCommissionRule;
using Aizen.Modules.Payment.Application.Commands.DeactivateCommissionRule;
using Aizen.Modules.Payment.Application.Commands.ReactivateCommissionRule;
using Aizen.Modules.Payment.Application.Commands.UpdateCommissionRule;
using Aizen.Modules.Payment.Application.Queries.GetCommissionRuleById;
using Aizen.Modules.Payment.Application.Queries.GetCommissionRulesList;
using Aizen.Modules.Payment.Application.Queries.GetCommissionRuleStats;
using Aizen.Modules.Payment.Application.Queries.ResolveCommissionRate;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.Payment.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/payment/commission")]
public sealed class CommissionRuleController : ControllerBase
{
    private readonly ISender _sender;
    public CommissionRuleController(ISender sender) => _sender = sender;

    // ── Queries ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns paged list of commission rules with optional filters.
    /// </summary>
    [HttpGet("rules")]
    public async Task<IActionResult> GetList(
        [FromQuery] CommissionRuleType?     ruleType = null,
        [FromQuery] CommissionRuleStatus?   status   = null,
        [FromQuery] CommissionRulePriority? priority = null,
        [FromQuery] int page     = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetCommissionRulesListQuery
        {
            RuleType = ruleType,
            Status   = status,
            Priority = priority,
            Page     = page,
            PageSize = pageSize,
        }, ct);
        return Ok(result);
    }

    /// <summary>
    /// Returns the full detail of a single commission rule by ID.
    /// </summary>
    [HttpGet("rules/{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetCommissionRuleByIdQuery { Id = id }, ct);
        return Ok(result);
    }

    /// <summary>
    /// Returns KPI statistics for the commission rules dashboard strip.
    /// </summary>
    [HttpGet("rules/stats")]
    public async Task<IActionResult> GetStats(CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetCommissionRuleStatsQuery(), ct);
        return Ok(result);
    }

    /// <summary>
    /// Resolves and returns the effective commission rate for the given context.
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

    // ── Commands ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Admin creates a new commission rule.
    /// </summary>
    [HttpPost("rules")]
    public async Task<IActionResult> Create(
        [FromBody] CreateCommissionRuleCommand command,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(command, ct);
        return Ok(result);
    }

    /// <summary>
    /// Admin updates the mutable fields of an existing commission rule.
    /// </summary>
    [HttpPut("rules/{id:long}")]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] UpdateCommissionRuleCommand command,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new UpdateCommissionRuleCommand
        {
            Id             = id,
            CommissionRate = command.CommissionRate,
            EffectiveFrom  = command.EffectiveFrom,
            EffectiveTo    = command.EffectiveTo,
            Priority       = command.Priority,
            Notes          = command.Notes,
        }, ct);
        return Ok(result);
    }

    /// <summary>
    /// Admin explicitly deactivates a commission rule.
    /// </summary>
    [HttpDelete("rules/{id:long}")]
    public async Task<IActionResult> Deactivate(
        long id,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new DeactivateCommissionRuleCommand { Id = id }, ct);
        return Ok(result);
    }

    /// <summary>
    /// Admin re-activates an Inactive commission rule.
    /// Status is re-derived from effective dates (Active, Scheduled, or Expired).
    /// </summary>
    [HttpPost("rules/{id:long}/reactivate")]
    public async Task<IActionResult> Reactivate(
        long id,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new ReactivateCommissionRuleCommand { Id = id }, ct);
        return Ok(result);
    }
}
