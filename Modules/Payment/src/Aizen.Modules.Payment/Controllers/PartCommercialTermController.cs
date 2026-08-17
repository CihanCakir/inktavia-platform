using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Application.Commands.CreatePartCommercialTerm;
using Aizen.Modules.Payment.Application.Commands.DeactivatePartCommercialTerm;
using Aizen.Modules.Payment.Application.Commands.ReactivatePartCommercialTerm;
using Aizen.Modules.Payment.Application.Commands.UpdatePartCommercialTerm;
using Aizen.Modules.Payment.Application.Queries.GetPartCommercialTermById;
using Aizen.Modules.Payment.Application.Queries.GetPartCommercialTermsList;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.Payment.Controllers;

/// <summary>
/// BE-S5a — admin surface for versioned/scoped part commercial terms. Admin-only. This is the Payment-internal configuration
/// surface (the admin sets the confidential cost here); the cost never leaves the module to SR/FE — only the cost-free
/// allowance does (see the internal <c>part-terms/resolve-lines</c> endpoint). Mirrors the P3/P5/P6 admin controllers.
/// </summary>
[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/payment/part-commercial-term")]
public sealed class PartCommercialTermController : ControllerBase
{
    private readonly ISender _sender;
    public PartCommercialTermController(ISender sender) => _sender = sender;

    [HttpGet("rules")]
    public async Task<IActionResult> GetList(
        [FromQuery] string? brand = null, [FromQuery] string? productCode = null,
        [FromQuery] long? providerProfileId = null, [FromQuery] string? categoryCode = null,
        [FromQuery] string? currencyCode = null, [FromQuery] bool? isActive = null, CancellationToken ct = default)
        => Ok(await _sender.Send(new GetPartCommercialTermsListQuery
        {
            Brand = brand, ProductCode = productCode, ProviderProfileId = providerProfileId,
            CategoryCode = categoryCode, CurrencyCode = currencyCode, IsActive = isActive,
        }, ct));

    [HttpGet("rules/{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct = default)
        => Ok(await _sender.Send(new GetPartCommercialTermByIdQuery { Id = id }, ct));

    [HttpPost("rules")]
    public async Task<IActionResult> Create([FromBody] CreatePartCommercialTermCommand command, CancellationToken ct = default)
        => Ok(await _sender.Send(command, ct));

    [HttpPut("rules/{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdatePartCommercialTermCommand command, CancellationToken ct = default)
    {
        if (id != command.Id) return BadRequest("Route id and body id must match.");
        return Ok(await _sender.Send(command, ct));
    }

    [HttpPost("rules/{id:long}/deactivate")]
    public async Task<IActionResult> Deactivate(long id, CancellationToken ct = default)
        => Ok(await _sender.Send(new DeactivatePartCommercialTermCommand { Id = id }, ct));

    [HttpPost("rules/{id:long}/reactivate")]
    public async Task<IActionResult> Reactivate(long id, CancellationToken ct = default)
        => Ok(await _sender.Send(new ReactivatePartCommercialTermCommand { Id = id }, ct));
}
