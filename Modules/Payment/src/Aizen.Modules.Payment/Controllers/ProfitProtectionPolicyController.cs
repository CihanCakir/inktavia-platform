using Aizen.Modules.Payment.Application.Commands.CreateProfitProtectionPolicy;
using Aizen.Modules.Payment.Application.Commands.DeactivateProfitProtectionPolicy;
using Aizen.Modules.Payment.Application.Commands.UpdateProfitProtectionPolicy;
using Aizen.Modules.Payment.Application.Queries.ResolveProfitProtectionPolicy;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.Payment.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/payment/profit-protection")]
public sealed class ProfitProtectionPolicyController : ControllerBase
{
    private readonly ISender _sender;
    public ProfitProtectionPolicyController(ISender sender) => _sender = sender;

    /// <summary>Resolves the single active policy for a currency (defaults to now).</summary>
    [HttpGet("resolve")]
    public async Task<IActionResult> Resolve(
        [FromQuery] string currencyCode = "TRY",
        [FromQuery] DateTime? atUtc = null,
        CancellationToken ct = default)
        => Ok(await _sender.Send(new ResolveProfitProtectionPolicyQuery { CurrencyCode = currencyCode, AtUtc = atUtc }, ct));

    [HttpPost("policies")]
    public async Task<IActionResult> Create(
        [FromBody] CreateProfitProtectionPolicyCommand command, CancellationToken ct = default)
        => Ok(await _sender.Send(command, ct));

    [HttpPut("policies/{id:long}")]
    public async Task<IActionResult> Update(
        long id, [FromBody] UpdateProfitProtectionPolicyCommand command, CancellationToken ct = default)
    {
        if (id != command.Id) return BadRequest("Route id and body id must match.");
        return Ok(await _sender.Send(command, ct));
    }

    [HttpPost("policies/{id:long}/deactivate")]
    public async Task<IActionResult> Deactivate(long id, CancellationToken ct = default)
        => Ok(await _sender.Send(new DeactivateProfitProtectionPolicyCommand { Id = id }, ct));
}
