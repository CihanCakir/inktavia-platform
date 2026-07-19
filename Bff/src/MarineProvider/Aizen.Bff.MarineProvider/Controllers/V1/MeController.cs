using Aizen.Bff.MarineProvider.Application.Common.Authorization;
using Aizen.Bff.MarineProvider.Application.Contracts.Me;
using Aizen.Bff.MarineProvider.Application.Contracts.Phone;
using Aizen.Bff.MarineProvider.Application.Me;
using Aizen.Bff.MarineProvider.Application.Me;
using Aizen.Bff.MarineProvider.Application.Me;
using Aizen.Bff.MarineProvider.Application.Phone;
using Aizen.Bff.MarineProvider.Application.Phone;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.MarineProvider.Controllers.V1;

[ApiController]
[Route("api/v1/provider/me")]
[Tags("Provider - Me")]
[Authorize(Policy = ProviderAuthorizationPolicies.ProviderAuthenticated)]
public sealed class MeController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public MeController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    /// <summary>Identity echo from the verified Keycloak token.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(GetProviderMeResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetProviderMeResponse>> GetMe(CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new GetProviderMeQuery(), ct);
        return SetResponse(result);
    }

    /// <summary>Provider-safe profile (works for pending providers; admin-only fields hidden).</summary>
    [HttpGet("profile")]
    [ProducesResponseType(typeof(GetProviderProfileResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetProviderProfileResponse>> GetProfile(CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new GetProviderProfileQuery(), ct);
        return SetResponse(result);
    }

    /// <summary>Account status gate. Works for pending/non-active providers; runtime Identity status.</summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(GetProviderStatusResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetProviderStatusResponse>> GetStatus(CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new GetProviderStatusQuery(), ct);
        return SetResponse(result);
    }

    /// <summary>Send an optional phone verification OTP (Identity SMS OTP; not a login method).</summary>
    [HttpPost("phone/send-otp")]
    [ProducesResponseType(typeof(SendProviderPhoneOtpResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<SendProviderPhoneOtpResponse>> SendPhoneOtp(
        [FromBody] SendProviderPhoneOtpCommand command, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(command, ct);
        return SetResponse(result);
    }

    /// <summary>Verify a phone OTP code. phoneVerified persistence is a documented Identity gap.</summary>
    [HttpPost("phone/verify-otp")]
    [ProducesResponseType(typeof(VerifyProviderPhoneOtpResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<VerifyProviderPhoneOtpResponse>> VerifyPhoneOtp(
        [FromBody] VerifyProviderPhoneOtpCommand command, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(command, ct);
        return SetResponse(result);
    }
}
