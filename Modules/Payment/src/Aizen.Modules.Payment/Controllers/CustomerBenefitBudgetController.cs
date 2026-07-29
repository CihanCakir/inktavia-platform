using Aizen.Modules.Payment.Application.Commands.BenefitBudget;
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
