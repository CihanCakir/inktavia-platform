using Aizen.Modules.Payment.Application.Commands.CreateProviderPlan;
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
            MaxActiveOffers  = command.MaxActiveOffers,
            HasPriorityBoost = command.HasPriorityBoost,
            HasFullAnalytics = command.HasFullAnalytics,
            SortOrder        = command.SortOrder,
        };
        var result = await _sender.Send(enriched, ct);
        return Ok(result);
    }
}
