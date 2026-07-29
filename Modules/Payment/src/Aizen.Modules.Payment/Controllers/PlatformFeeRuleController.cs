using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Application.Commands.CreatePlatformFeeRule;
using Aizen.Modules.Payment.Application.Commands.DeactivatePlatformFeeRule;
using Aizen.Modules.Payment.Application.Commands.UpdatePlatformFeeRule;
using Aizen.Modules.Payment.Application.Queries.ResolvePlatformFee;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.Payment.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/payment/platform-fee")]
public sealed class PlatformFeeRuleController : ControllerBase
{
    private readonly ISender _sender;
    public PlatformFeeRuleController(ISender sender) => _sender = sender;

    // ── Query: resolve (admin/dev preview) ──────────────────────────────────────

    /// <summary>Resolves the platform fee rule for a context and computes net/vat/gross on the supplied base.</summary>
    [HttpGet("resolve")]
    public async Task<IActionResult> Resolve(
        [FromQuery] string  currencyCode = "TRY",
        [FromQuery] string? categoryCode = null,
        [FromQuery] string? customerType = null,
        [FromQuery] decimal customerPayableServiceAmount = 0m,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new ResolvePlatformFeeQuery
        {
            CurrencyCode                 = currencyCode,
            CategoryCode                 = categoryCode,
            CustomerType                 = customerType,
            CustomerPayableServiceAmount = customerPayableServiceAmount,
        }, ct);
        return Ok(result);
    }

    // ── Commands ────────────────────────────────────────────────────────────────

    [HttpPost("rules")]
    public async Task<IActionResult> Create(
        [FromBody] CreatePlatformFeeRuleCommand command, CancellationToken ct = default)
        => Ok(await _sender.Send(command, ct));

    [HttpPut("rules/{id:long}")]
    public async Task<IActionResult> Update(
        long id, [FromBody] UpdatePlatformFeeRuleCommand command, CancellationToken ct = default)
    {
        if (id != command.Id) return BadRequest("Route id and body id must match.");
        return Ok(await _sender.Send(command, ct));
    }

    [HttpPost("rules/{id:long}/deactivate")]
    public async Task<IActionResult> Deactivate(long id, CancellationToken ct = default)
        => Ok(await _sender.Send(new DeactivatePlatformFeeRuleCommand { Id = id }, ct));
}
