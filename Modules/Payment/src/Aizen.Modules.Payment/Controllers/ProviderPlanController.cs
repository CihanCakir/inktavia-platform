using Aizen.Modules.Payment.Application.Commands.ActivateProviderPlan;
using Aizen.Modules.Payment.Application.Commands.CreateProviderPlan;
using Aizen.Modules.Payment.Application.Commands.DeactivateProviderPlan;
using Aizen.Modules.Payment.Application.Commands.UpdateProviderPlan;
using Aizen.Modules.Payment.Application.Queries.GetProviderPlanById;
using Aizen.Modules.Payment.Application.Queries.GetProviderPlans;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.Payment.Controllers;

[ApiController]
[Route("api/v1/payment/provider-plans")]
public sealed class ProviderPlanController : ControllerBase
{
    private readonly ISender _sender;
    public ProviderPlanController(ISender sender) => _sender = sender;

    [HttpGet]
    [AllowAnonymous] // Plans are publicly visible for marketing/pricing pages
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _sender.Send(new GetProviderPlansQuery(), ct);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await _sender.Send(new GetProviderPlanByIdQuery { Id = id }, ct);
        if (result is null) return NotFound();
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = "AdminPanelAccess")]
    public async Task<IActionResult> Create([FromBody] CreateProviderPlanCommand command, CancellationToken ct)
    {
        var result = await _sender.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = result!.Id }, result);
    }

    /// <summary>
    /// Updates all editable fields of a provider subscription plan.
    /// PlanCode is immutable after creation.
    /// </summary>
    [HttpPut("{id:long}")]
    [Authorize(Policy = "AdminPanelAccess")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateProviderPlanCommand command, CancellationToken ct)
    {
        var enriched = new UpdateProviderPlanCommand
        {
            Id               = id,
            Name             = command.Name,
            Description      = command.Description,
            MonthlyPriceTRY  = command.MonthlyPriceTRY,
            AnnualPriceTRY   = command.AnnualPriceTRY,
            TrialDays        = command.TrialDays,
            BadgeLabel       = command.BadgeLabel,
            MaxActiveOffers  = command.MaxActiveOffers,
            HasPriorityBoost = command.HasPriorityBoost,
            HasFullAnalytics = command.HasFullAnalytics,
            SortOrder        = command.SortOrder,
            ValidFrom        = command.ValidFrom,
            ValidTo          = command.ValidTo,
            FeatureItems     = command.FeatureItems,
        };
        var result = await _sender.Send(enriched, ct);
        return Ok(result);
    }

    /// <summary>
    /// Sets IsActive=true. Does NOT create subscriptions or trigger billing.
    /// </summary>
    [HttpPost("{id:long}/activate")]
    [Authorize(Policy = "AdminPanelAccess")]
    public async Task<IActionResult> Activate(long id, CancellationToken ct)
    {
        var result = await _sender.Send(new ActivateProviderPlanCommand { Id = id }, ct);
        return Ok(result);
    }

    /// <summary>
    /// Sets IsActive=false. Does NOT cancel existing subscriptions or affect billing.
    /// </summary>
    [HttpPost("{id:long}/deactivate")]
    [Authorize(Policy = "AdminPanelAccess")]
    public async Task<IActionResult> Deactivate(long id, CancellationToken ct)
    {
        var result = await _sender.Send(new DeactivateProviderPlanCommand { Id = id }, ct);
        return Ok(result);
    }
}
