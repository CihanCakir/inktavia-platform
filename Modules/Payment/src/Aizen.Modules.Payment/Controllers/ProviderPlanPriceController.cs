using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Application.Commands.CreateProviderPlanPrice;
using Aizen.Modules.Payment.Application.Commands.DeactivateProviderPlanPrice;
using Aizen.Modules.Payment.Application.Commands.UpdateProviderPlanPrice;
using Aizen.Modules.Payment.Application.Queries.GetProviderPlanPrices;
using Aizen.Modules.Payment.Application.Queries.GetSubscriptionsWithUpcomingPriceChange;
using Aizen.Modules.Payment.Application.Queries.ResolveProviderPlanPrice;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.Payment.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/payment/plan-prices")]
public sealed class ProviderPlanPriceController : ControllerBase
{
    private readonly ISender _sender;
    public ProviderPlanPriceController(ISender sender) => _sender = sender;

    // ── Queries ─────────────────────────────────────────────────────────────────

    [HttpGet("plan/{planId:long}")]
    public async Task<IActionResult> GetForPlan(long planId, CancellationToken ct = default)
        => Ok(await _sender.Send(new GetProviderPlanPricesQuery { ProviderPlanId = planId }, ct));

    /// <summary>Resolves the single active price at an instant (defaults to now).</summary>
    [HttpGet("resolve")]
    public async Task<IActionResult> Resolve(
        [FromQuery] long          providerPlanId,
        [FromQuery] string        currencyCode  = "TRY",
        [FromQuery] BillingPeriod billingPeriod = BillingPeriod.Monthly,
        [FromQuery] DateTime?     atUtc         = null,
        CancellationToken ct = default)
        => Ok(await _sender.Send(new ResolveProviderPlanPriceQuery
        {
            ProviderPlanId = providerPlanId,
            CurrencyCode   = currencyCode,
            BillingPeriod  = billingPeriod,
            AtUtc          = atUtc,
        }, ct));

    /// <summary>Active auto-renewing subscriptions whose renewal price differs, renewing within N days (feeds N1).</summary>
    [HttpGet("upcoming-changes")]
    public async Task<IActionResult> UpcomingChanges([FromQuery] int withinDays = 14, CancellationToken ct = default)
        => Ok(await _sender.Send(new GetSubscriptionsWithUpcomingPriceChangeQuery { WithinDays = withinDays }, ct));

    // ── Commands ────────────────────────────────────────────────────────────────

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateProviderPlanPriceCommand command, CancellationToken ct = default)
        => Ok(await _sender.Send(command, ct));

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(
        long id, [FromBody] UpdateProviderPlanPriceCommand command, CancellationToken ct = default)
    {
        if (id != command.Id) return BadRequest("Route id and body id must match.");
        return Ok(await _sender.Send(command, ct));
    }

    [HttpPost("{id:long}/deactivate")]
    public async Task<IActionResult> Deactivate(long id, CancellationToken ct = default)
        => Ok(await _sender.Send(new DeactivateProviderPlanPriceCommand { Id = id }, ct));
}
