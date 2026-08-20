using Aizen.Bff.MarineProvider.Application.Auth;
using Aizen.Bff.MarineProvider.Application.Contracts.Auth.EmailVerification;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Aizen.Bff.MarineProvider.Controllers.V1.Auth;

/// <summary>
/// Sağlayıcı e-posta doğrulama cephesi (public/anonim — kullanıcı e-postadaki linke tıklar, henüz oturum yok).
/// BFF tüm token mantığını Identity'ye devreder; onaydan SONRA Keycloak'ta emailVerified=true yapar (bkz.
/// VerifyEmailCommandHandler — sıra: önce bizim DB, sonra Keycloak). İnce controller.
/// </summary>
[ApiController]
[Route("api/v1/provider/auth/verify-email")]
[Tags("Provider - Email Verification")]
[AllowAnonymous]
[EnableRateLimiting("email-verify-ip")]
public sealed class EmailVerificationController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public EmailVerificationController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    /// <summary>Linkteki token'ı onaylar. Süresi dolmuş vs geçersiz ayrımını Status ile döner (FE dallanır).</summary>
    [HttpPost("")]
    [ProducesResponseType(typeof(VerifyEmailResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<VerifyEmailResponse>> Verify(
        [FromBody] VerifyEmailRequest request, CancellationToken ct)
    {
        var command = new VerifyEmailCommand { Token = request.Token };
        var result = await _cqrs.ProcessAsync(command, ct);
        return SetResponse(result);
    }

    /// <summary>Doğrulama e-postasını yeniden gönderir. Numaralandırma korumalı (genel yanıt).</summary>
    [HttpPost("resend")]
    [ProducesResponseType(typeof(ResendVerifyEmailResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ResendVerifyEmailResponse>> Resend(
        [FromBody] ResendVerifyEmailRequest request, CancellationToken ct)
    {
        var command = new ResendVerifyEmailCommand { Email = request.Email };
        var result = await _cqrs.ProcessAsync(command, ct);
        return SetResponse(result);
    }
}
