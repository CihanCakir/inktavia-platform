using Aizen.Modules.Payment.Application.Commands.CreateParticipantPlan;
using Aizen.Modules.Payment.Application.Commands.UpdateParticipantPlan;
using Aizen.Modules.Payment.Application.Queries.GetParticipantPlanById;
using Aizen.Modules.Payment.Application.Queries.GetParticipantPlans;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.Payment.Controllers;

[ApiController]
[Route("api/v1/payment/participant-plans")]
public sealed class ParticipantPlanController : ControllerBase
{
    private readonly ISender _sender;
    public ParticipantPlanController(ISender sender) => _sender = sender;

    [HttpGet]
    [AllowAnonymous] // Plans are publicly visible
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _sender.Send(new GetParticipantPlansQuery(), ct);
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
    [Authorize(Policy = "AdminPanelAccess")]
    public async Task<IActionResult> Create([FromBody] CreateParticipantPlanCommand command, CancellationToken ct)
    {
        var result = await _sender.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = result!.Id }, result);
    }

    [HttpPut("{id:long}")]
    [Authorize(Policy = "AdminPanelAccess")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateParticipantPlanCommand command, CancellationToken ct)
    {
        var enriched = new UpdateParticipantPlanCommand
        {
            Id                    = id,
            Name                  = command.Name,
            Description           = command.Description,
            MonthlyPriceTRY       = command.MonthlyPriceTRY,
            ServiceDiscountRate   = command.ServiceDiscountRate,
            CargoDryDiscountRate  = command.CargoDryDiscountRate,
            InkCoinEarnMultiplier = command.InkCoinEarnMultiplier,
            SortOrder             = command.SortOrder,
        };
        var result = await _sender.Send(enriched, ct);
        return Ok(result);
    }
}
