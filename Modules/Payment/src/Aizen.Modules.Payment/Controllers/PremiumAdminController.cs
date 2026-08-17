using Aizen.Modules.Payment.Application.Commands.PremiumAdmin;
using Aizen.Modules.Payment.Application.Queries.PremiumAdmin;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.Payment.Controllers;

/// <summary>BE-P11 — admin CRUD for premium products + versioned prices (point-in-time, overlap-guarded).</summary>
[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/payment/admin/premium")]
public sealed class PremiumAdminController : ControllerBase
{
    private readonly ISender _sender;
    public PremiumAdminController(ISender sender) => _sender = sender;

    // ── Products ──────────────────────────────────────────────────────────────

    [HttpGet("products")]
    public async Task<IActionResult> GetProducts(CancellationToken ct)
        => Ok(await _sender.Send(new GetPremiumProductsQuery(), ct));

    [HttpGet("products/{id:long}")]
    public async Task<IActionResult> GetProduct(long id, CancellationToken ct)
        => Ok(await _sender.Send(new GetPremiumProductByIdQuery { Id = id }, ct));

    [HttpPost("products")]
    public async Task<IActionResult> CreateProduct([FromBody] CreatePremiumProductCommand command, CancellationToken ct)
        => Ok(await _sender.Send(command, ct));

    [HttpPut("products/{id:long}")]
    public async Task<IActionResult> UpdateProduct(long id, [FromBody] UpdatePremiumProductCommand body, CancellationToken ct)
        => Ok(await _sender.Send(new UpdatePremiumProductCommand
        {
            Id = id, Name = body.Name, DurationDays = body.DurationDays, Description = body.Description,
        }, ct));

    [HttpPost("products/{id:long}/activate")]
    public async Task<IActionResult> ActivateProduct(long id, CancellationToken ct)
        => Ok(await _sender.Send(new SetPremiumProductStatusCommand { Id = id, Activate = true }, ct));

    [HttpPost("products/{id:long}/deactivate")]
    public async Task<IActionResult> DeactivateProduct(long id, CancellationToken ct)
        => Ok(await _sender.Send(new SetPremiumProductStatusCommand { Id = id, Activate = false }, ct));

    // ── Prices ────────────────────────────────────────────────────────────────

    [HttpGet("products/{productId:long}/prices")]
    public async Task<IActionResult> GetPrices(long productId, CancellationToken ct)
        => Ok(await _sender.Send(new GetPremiumProductPricesQuery { PremiumProductId = productId }, ct));

    [HttpGet("products/{productId:long}/prices/resolve")]
    public async Task<IActionResult> ResolvePrice(long productId, [FromQuery] string currencyCode = "TRY",
        [FromQuery] DateTime? atUtc = null, CancellationToken ct = default)
        => Ok(await _sender.Send(new ResolvePremiumProductPriceQuery { PremiumProductId = productId, CurrencyCode = currencyCode, AtUtc = atUtc }, ct));

    [HttpPost("prices")]
    public async Task<IActionResult> CreatePrice([FromBody] CreatePremiumProductPriceCommand command, CancellationToken ct)
        => Ok(await _sender.Send(command, ct));

    [HttpPut("prices/{id:long}")]
    public async Task<IActionResult> UpdatePrice(long id, [FromBody] UpdatePremiumProductPriceCommand body, CancellationToken ct)
        => Ok(await _sender.Send(new UpdatePremiumProductPriceCommand
        {
            Id = id, PriceAmount = body.PriceAmount, EffectiveFrom = body.EffectiveFrom, EffectiveTo = body.EffectiveTo, Notes = body.Notes,
        }, ct));

    [HttpPost("prices/{id:long}/deactivate")]
    public async Task<IActionResult> DeactivatePrice(long id, CancellationToken ct)
        => Ok(await _sender.Send(new DeactivatePremiumProductPriceCommand { Id = id }, ct));
}
