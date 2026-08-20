using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Identity.Abstraction.Dto.EmailVerification;
using Aizen.Modules.InktaviaStore.Application.Identity.Command.EmailVerification.GenerateProviderEmailVerification;
using Aizen.Modules.InktaviaStore.Application.Identity.Command.EmailVerification.ConfirmProviderEmailVerification;
using Aizen.Modules.InktaviaStore.Application.Identity.Command.EmailVerification.ResendProviderEmailVerification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.InktaviaStore.Controller.V1.Identity;

/// <summary>
/// Sağlayıcı e-posta doğrulama (Identity sahipliğinde, ASP.NET Core Identity YERLEŞİK onay token'ı). Servis-token
/// yetkili (IdentityWrite), MarineProvider BFF çağırır. Uçlar HTTP <c>Request</c> DTO'larını iç CQRS komutlarına eşler.
///
/// Token idempotent olduğundan custom tasarımın verify/consume ayrımı YOK — tek <c>confirm</c> ucu vardır ve
/// tekrar çağrılması güvenlidir. Akış (BFF): <c>confirm</c> (bizim DB, EmailConfirmed=true) → Keycloak emailVerified=true.
/// Keycloak patlarsa kullanıcı aynı linke yeniden tıklar. Hiçbir uç oturum/çerez/JWT üretmez.
/// </summary>
[ApiController]
[Route("api/v1/identity/auth/provider-email-verification")]
[Tags("Identity - Provider Email Verification")]
[Authorize(Policy = "IdentityWrite")]
public sealed class ProviderEmailVerificationController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _sender;

    public ProviderEmailVerificationController(
        IHttpContextAccessor httpContextAccessor,
        IAizenCQRSProcessor cqrsProcessor) : base(httpContextAccessor)
    {
        _sender = cqrsProcessor;
    }

    [HttpPost("generate")]
    [ProducesResponseType(typeof(GenerateProviderEmailVerificationResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GenerateProviderEmailVerificationResponse>> Generate(
        [FromBody] GenerateProviderEmailVerificationRequest request, CancellationToken ct)
    {
        var command = new GenerateProviderEmailVerificationCommand { Email = request.Email };
        var result = await _sender.ProcessAsync(command, ct);
        return SetResponse(result);
    }

    [HttpPost("confirm")]
    [ProducesResponseType(typeof(ConfirmProviderEmailVerificationResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ConfirmProviderEmailVerificationResponse>> Confirm(
        [FromBody] ConfirmProviderEmailVerificationRequest request, CancellationToken ct)
    {
        var command = new ConfirmProviderEmailVerificationCommand { Token = request.Token };
        var result = await _sender.ProcessAsync(command, ct);
        return SetResponse(result);
    }

    [HttpPost("resend")]
    [ProducesResponseType(typeof(ResendProviderEmailVerificationResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ResendProviderEmailVerificationResponse>> Resend(
        [FromBody] ResendProviderEmailVerificationRequest request, CancellationToken ct)
    {
        var command = new ResendProviderEmailVerificationCommand { Email = request.Email };
        var result = await _sender.ProcessAsync(command, ct);
        return SetResponse(result);
    }
}
