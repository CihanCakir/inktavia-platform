using Aizen.Modules.Payment.Application.Commands.ActivateParticipantPlan;
using Aizen.Modules.Payment.Application.Commands.CreateParticipantPlan;
using Aizen.Modules.Payment.Application.Commands.DeactivateParticipantPlan;
using Aizen.Modules.Payment.Application.Commands.UpdateParticipantPlan;
using Aizen.Modules.Payment.Application.Queries.GetParticipantPlanById;
using Aizen.Modules.Payment.Application.Queries.GetParticipantPlans;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.Payment.Controllers;

[ApiController]
[Route("api/v1/payment/participant-plans")]
[Authorize(Roles = "Admin")] // Writes require Admin; GETs opt out with [AllowAnonymous]
public sealed class ParticipantPlanController : ControllerBase
{
    private readonly ISender _sender;
    public ParticipantPlanController(ISender sender) => _sender = sender;

    [HttpGet]
    [AllowAnonymous] // Plans are publicly visible
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false, CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetParticipantPlansQuery { IncludeInactive = includeInactive }, ct);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await _sender.Send(new GetParticipantPlanByIdQuery { Id = id }, ct);
        if (result is null) return NotFound();
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateParticipantPlanCommand command, CancellationToken ct)
    {
        var result = await _sender.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = result!.Id }, result);
    }

    /// <summary>
    /// Updates all editable fields of a participant subscription plan.
    /// PlanCode is immutable after creation.
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateParticipantPlanCommand command, CancellationToken ct)
    {
        var enriched = new UpdateParticipantPlanCommand
        {
            Id                    = id,
            Name                  = command.Name,
            Description           = command.Description,
            MonthlyPriceTRY       = command.MonthlyPriceTRY,
            AnnualPriceTRY        = command.AnnualPriceTRY,
            TrialDays             = command.TrialDays,
            BadgeLabel            = command.BadgeLabel,
            ServiceDiscountRate   = command.ServiceDiscountRate,
            CargoDryDiscountRate  = command.CargoDryDiscountRate,
            InkCoinEarnMultiplier = command.InkCoinEarnMultiplier,
            SortOrder             = command.SortOrder,
            ValidFrom             = command.ValidFrom,
            ValidTo               = command.ValidTo,
            FeatureItems          = command.FeatureItems,
        };
        var result = await _sender.Send(enriched, ct);
        return Ok(result);
    }

    /// <summary>
    /// Sets IsActive=true. Does NOT create subscriptions or trigger billing.
    /// </summary>
    [HttpPost("{id:long}/activate")]
    public async Task<IActionResult> Activate(long id, CancellationToken ct)
    {
        var result = await _sender.Send(new ActivateParticipantPlanCommand { Id = id }, ct);
        return Ok(result);
    }

    /// <summary>
    /// Sets IsActive=false. Does NOT cancel existing subscriptions or affect billing.
    /// </summary>
    [HttpPost("{id:long}/deactivate")]
    public async Task<IActionResult> Deactivate(long id, CancellationToken ct)
    {
        var result = await _sender.Send(new DeactivateParticipantPlanCommand { Id = id }, ct);
        return Ok(result);
    }
}
