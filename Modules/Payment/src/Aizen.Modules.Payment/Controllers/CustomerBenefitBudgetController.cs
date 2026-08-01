using Aizen.Modules.Payment.Application.Commands.BenefitBudget;
using Aizen.Modules.Payment.Application.Queries.GetCustomerBenefitBudgetPoliciesList;
using Aizen.Modules.Payment.Application.Queries.GetCustomerBenefitBudgetPolicyById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.Payment.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/payment/benefit-budget")]
public sealed class CustomerBenefitBudgetController : ControllerBase
{
    private readonly ISender _sender;
    public CustomerBenefitBudgetController(ISender sender) => _sender = sender;

    /// <summary>Returns the per-plan benefit budget policy list (no paging), optionally filtered by plan / currency / active.</summary>
    [HttpGet("policies")]
    public async Task<IActionResult> GetList(
        [FromQuery] long?   customerPlanId = null,
        [FromQuery] string? currencyCode   = null,
        [FromQuery] bool?   isActive       = null,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetCustomerBenefitBudgetPoliciesListQuery
        {
            CustomerPlanId = customerPlanId,
            CurrencyCode   = currencyCode,
            IsActive       = isActive,
        }, ct);
        return Ok(result);
    }

    /// <summary>Returns the full detail of a single benefit budget policy by ID.</summary>
    [HttpGet("policies/{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct = default)
        => Ok(await _sender.Send(new GetCustomerBenefitBudgetPolicyByIdQuery { Id = id }, ct));

    [HttpPost("policies")]
    public async Task<IActionResult> CreatePolicy(
        [FromBody] CreateCustomerBenefitBudgetPolicyCommand command, CancellationToken ct = default)
        => Ok(await _sender.Send(command, ct));

    [HttpPost("reserve")]
    public async Task<IActionResult> Reserve(
        [FromBody] ReserveBenefitCommand command, CancellationToken ct = default)
        => Ok(await _sender.Send(command, ct));

    [HttpPost("reservations/{reservationId:long}/consume")]
    public async Task<IActionResult> Consume(long reservationId, CancellationToken ct = default)
        => Ok(await _sender.Send(new ConsumeBenefitCommand { ReservationId = reservationId }, ct));

    [HttpPost("reservations/{reservationId:long}/release")]
    public async Task<IActionResult> Release(long reservationId, CancellationToken ct = default)
        => Ok(await _sender.Send(new ReleaseBenefitCommand { ReservationId = reservationId }, ct));
}
