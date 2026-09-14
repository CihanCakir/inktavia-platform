using Aizen.Modules.CargoDry.Application.Queries.GetCargoDryCatalogProducts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.CargoDry.Controllers;

/// <summary>
/// Owner-safe CargoDry product catalog (CargoDry supply flow). Same [Authorize] posture as the kits controller the
/// mobile BFF already calls, so the BFF service token + assertion reaches it. Returns the raw owner-safe DTO — no
/// commercial pricing ever crosses this surface.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/cargodry/catalog")]
public sealed class CargoDryCatalogController : ControllerBase
{
    private readonly ISender _sender;

    public CargoDryCatalogController(ISender sender) => _sender = sender;

    [HttpGet("products")]
    public async Task<IActionResult> GetProducts(CancellationToken ct)
    {
        var result = await _sender.Send(new GetCargoDryCatalogProductsQuery(), ct);
        return Ok(result);
    }
}
