using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Application.Commands.CreateProfitProtectionPolicy;
using Aizen.Modules.Payment.Application.Commands.DeactivateProfitProtectionPolicy;
using Aizen.Modules.Payment.Application.Commands.ReactivateProfitProtectionPolicy;
using Aizen.Modules.Payment.Application.Commands.UpdateProfitProtectionPolicy;
using Aizen.Modules.Payment.Application.Queries.GetProfitProtectionPoliciesList;
using Aizen.Modules.Payment.Application.Queries.GetProfitProtectionPolicyById;
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

    /// <summary>Returns the policy version history (no paging), optionally filtered by currency / status / active.</summary>
    [HttpGet("policies")]
    public async Task<IActionResult> GetList(
        [FromQuery] string?               currencyCode = null,
        [FromQuery] CommissionRuleStatus? status       = null,
        [FromQuery] bool?                 isActive     = null,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetProfitProtectionPoliciesListQuery
        {
            CurrencyCode = currencyCode,
            Status       = status,
            IsActive     = isActive,
        }, ct);
        return Ok(result);
    }

    /// <summary>Returns the full detail of a single profit-protection policy by ID.</summary>
    [HttpGet("policies/{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct = default)
        => Ok(await _sender.Send(new GetProfitProtectionPolicyByIdQuery { Id = id }, ct));

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

    /// <summary>Admin re-activates an Inactive policy (re-checks the single-active overlap guard for the currency).</summary>
    [HttpPost("policies/{id:long}/reactivate")]
    public async Task<IActionResult> Reactivate(long id, CancellationToken ct = default)
        => Ok(await _sender.Send(new ReactivateProfitProtectionPolicyCommand { Id = id }, ct));
}
