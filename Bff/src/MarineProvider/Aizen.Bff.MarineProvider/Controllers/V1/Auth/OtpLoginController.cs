using Aizen.Bff.MarineProvider.Application.Auth;
using Aizen.Bff.MarineProvider.Application.Contracts.Auth.OtpLogin;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Aizen.Bff.MarineProvider.Controllers.V1.Auth;

[ApiController]
[Route("api/v1/provider/auth/otp-login")]
[Tags("Provider - OTP Login")]
[AllowAnonymous]
[EnableRateLimiting("pwd-recovery-ip")]
public sealed class OtpLoginController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public OtpLoginController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor) { _cqrs = cqrs; }

    [HttpPost("request")]
    [ProducesResponseType(typeof(OtpLoginRequestResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<OtpLoginRequestResponse>> Request(
        [FromBody] OtpLoginRequest request, CancellationToken ct)
    {
        var command = new RequestOtpLoginCommand { Channel = request.Channel, Identifier = request.Identifier };
        var result = await _cqrs.ProcessAsync(command, ct);
        return SetResponse(result);
    }

    [HttpPost("verify")]
    [ProducesResponseType(typeof(OtpLoginVerifyResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<OtpLoginVerifyResponse>> Verify(
        [FromBody] OtpLoginVerifyRequest request, CancellationToken ct)
    {
        var command = new VerifyOtpLoginCommand { LoginRequestId = request.LoginRequestId, OtpCode = request.OtpCode };
        var result = await _cqrs.ProcessAsync(command, ct);
        return SetResponse(result);
    }

    [HttpPost("resend")]
    [ProducesResponseType(typeof(OtpLoginResendResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<OtpLoginResendResponse>> Resend(
        [FromBody] OtpLoginResendRequest request, CancellationToken ct)
    {
        var command = new ResendOtpLoginCommand { LoginRequestId = request.LoginRequestId };
        var result = await _cqrs.ProcessAsync(command, ct);
        return SetResponse(result);
    }
}
