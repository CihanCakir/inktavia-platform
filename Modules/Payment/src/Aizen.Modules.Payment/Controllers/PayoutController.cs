using Aizen.Modules.Payment.Abstraction.Requests;
using Aizen.Modules.Payment.Application.Commands.MarkPayoutComplete;
using Aizen.Modules.Payment.Application.Queries.GetPendingPayouts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.Payment.Controllers;

[ApiController]
[Authorize(Roles = "payment.admin")]
[Route("api/v1/payment/payouts")]
public sealed class PayoutController : ControllerBase
{
    private readonly ISender _sender;
    public PayoutController(ISender sender) => _sender = sender;

    [HttpGet("pending")]
    public async Task<IActionResult> GetPending(CancellationToken ct)
    {
        var result = await _sender.Send(new GetPendingPayoutsQuery(), ct);
        return Ok(result);
    }

    [HttpPost("{id:long}/complete")]
    public async Task<IActionResult> MarkComplete(
        long id, [FromBody] MarkPayoutCompleteRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new MarkPayoutCompleteCommand
        {
            PayoutRecordId  = id,
            GatewayPayoutId = request.GatewayPayoutId,
            AdminNote       = request.AdminNote,
        }, ct);
        return Ok(result);
    }
}
