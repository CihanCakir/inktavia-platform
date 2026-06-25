using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Application.Commands.ValidateKit;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Aizen.Modules.CargoDry.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/cargodry/public")]
public sealed class CargoDryPublicController : ControllerBase
{
    private readonly ISender _sender;
    public CargoDryPublicController(ISender sender) => _sender = sender;

    /// <summary>QR veya seri numarasını doğrula → 5 dakikalık activation token üret.</summary>
    [HttpPost("validate")]
    [EnableRateLimiting("validate-ip")]
    public async Task<IActionResult> ValidateKit(
        [FromBody] ValidateKitRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new ValidateKitCommand
        {
            SerialNumber = request.SerialNumber,
            BatchCode    = request.BatchCode,
            Signature    = request.Signature,
            Source       = ActivationSource.MobileApp,
        }, ct);
        return Ok(result);
    }
}

public sealed class ValidateKitRequest
{
    public string  SerialNumber { get; init; } = default!;
    public string  BatchCode    { get; init; } = default!;
    public string? Signature    { get; init; }
}
