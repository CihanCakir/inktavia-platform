using Aizen.Bff.AdminPanel.Application.Auth.Command;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Identity.Abstraction.Dto;
using Aizen.Modules.Identity.Abstraction.Request;
using Aizen.Modules.Identity.Abstraction.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.AdminPanel.Controllers.V1;

[ApiController]
[Route("api/v1/admin-panel/auth")]
[Tags("Admin Panel - Auth")]
public sealed class AuthController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public AuthController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrsProcessor)
        : base(httpContextAccessor)
    {
        _cqrs = cqrsProcessor;
    }

    [HttpPost("login/username")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(UserLoginResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<UserLoginResponse>> LoginWithUsername(
        [FromBody] LoginWithUsernameRequest req, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new LoginWithUsernameBffCommand(req), ct);
        return SetResponse(result);
    }

    [HttpPost("login/phone")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(UserLoginResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<UserLoginResponse>> LoginWithPhone(
        [FromBody] LoginWithPhoneRequest req, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new LoginWithPhoneBffCommand(req), ct);
        return SetResponse(result);
    }

    [HttpPost("login/otp")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(UserLoginResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<UserLoginResponse>> LoginWithOtp(
        [FromBody] LoginWithOtpRequest req, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new LoginWithOtpBffCommand(req), ct);
        return SetResponse(result);
    }

    [HttpPost("otp/send")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(SendOtpDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<SendOtpDto>> SendOtp(
        [FromBody] SendOtpRequest req, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new SendOtpBffCommand(req), ct);
        return SetResponse(result);
    }

    [HttpPost("otp/check")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CheckOtpDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CheckOtpDto>> CheckOtp(
        [FromBody] CheckOtpRequest req, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new CheckOtpBffCommand(req), ct);
        return SetResponse(result);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(UserLoginResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<UserLoginResponse>> Refresh(
        [FromBody] RefreshLoginHttpRequest req, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new RefreshBffCommand(req), ct);
        return SetResponse(result);
    }

    [HttpPost("password/change")]
    [Authorize(Policy = "AdminPanelAccess")]
    [ProducesResponseType(typeof(ChangePasswordDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ChangePasswordDto>> ChangePassword(
        [FromBody] ChangePasswordRequest req, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new ChangePasswordBffCommand(req), ct);
        return SetResponse(result);
    }
}
