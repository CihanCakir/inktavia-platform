using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Application.Commands.ActivateKit;
using Aizen.Modules.CargoDry.Application.Queries.GetMyKits;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.CargoDry.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/cargodry/kits")]
public sealed class CargoDryKitsController : ControllerBase
{
    private readonly ISender            _sender;
    private readonly IAizenInfoAccessor _info;

    public CargoDryKitsController(ISender sender, IAizenInfoAccessor info)
    {
        _sender = sender;
        _info   = info;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyKits(CancellationToken ct)
    {
        var userId = _info.UserInfoAccessor.UserInfo.UserId;
        var result = await _sender.Send(new GetMyKitsQuery { UserId = userId }, ct);
        return Ok(result);
    }

    [HttpPost("activate")]
    public async Task<IActionResult> ActivateKit(
        [FromBody] ActivateKitRequest request, CancellationToken ct)
    {
        var userId = _info.UserInfoAccessor.UserInfo.UserId;
        var result = await _sender.Send(new ActivateKitCommand
        {
            ActivationToken = request.ActivationToken,
            VesselId        = request.VesselId,
            UserId          = userId,
            Method          = request.Method,
            Source          = ActivationSource.MobileApp,
            IpAddress       = HttpContext.Connection.RemoteIpAddress?.ToString(),
            DeviceInfo      = Request.Headers.UserAgent.ToString(),
        }, ct);
        return Ok(result);
    }
}

public sealed class ActivateKitRequest
{
    public string           ActivationToken { get; init; } = default!;
    public long             VesselId        { get; init; }
    public ActivationMethod Method          { get; init; } = ActivationMethod.QrScan;
}
