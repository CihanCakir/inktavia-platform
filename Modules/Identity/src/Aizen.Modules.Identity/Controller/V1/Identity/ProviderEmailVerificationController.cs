using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Identity.Abstraction.Dto.EmailVerification;
using Aizen.Modules.InktaviaStore.Application.Identity.Command.EmailVerification.GenerateProviderEmailVerification;
using Aizen.Modules.InktaviaStore.Application.Identity.Command.EmailVerification.VerifyProviderEmailVerification;
using Aizen.Modules.InktaviaStore.Application.Identity.Command.EmailVerification.ConsumeProviderEmailVerification;
using Aizen.Modules.InktaviaStore.Application.Identity.Command.EmailVerification.ResendProviderEmailVerification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.InktaviaStore.Controller.V1.Identity;

/// <summary>
/// Sağlayıcı e-posta doğrulama (Identity sahipliğinde). Servis-token yetkili (IdentityWrite), MarineProvider BFF çağırır.
/// Uçlar HTTP <c>Request</c> DTO'larını bağlar ve iç CQRS komutlarına eşler — komut sınıfları HTTP sözleşmesi olarak açılmaz.
///
/// Doğrulama (<c>verify</c>) ve tüketim (<c>consume</c>) bilerek ayrıdır: BFF önce doğrular, Keycloak'ta emailVerified'ı
/// çevirir, EN SON tüketir (bkz. <c>IProviderEmailVerificationDomainService</c>). Hiçbir uç oturum/çerez/JWT üretmez.
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
        var command = new GenerateProviderEmailVerificationCommand
        {
            KeycloakSubjectId = request.KeycloakSubjectId,
            Email = request.Email,
        };

        var result = await _sender.ProcessAsync(command, ct);
        return SetResponse(result);
    }

    [HttpPost("verify")]
    [ProducesResponseType(typeof(VerifyProviderEmailVerificationResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<VerifyProviderEmailVerificationResponse>> Verify(
        [FromBody] VerifyProviderEmailVerificationRequest request, CancellationToken ct)
    {
        var command = new VerifyProviderEmailVerificationCommand
        {
            Token = request.Token,
        };

        var result = await _sender.ProcessAsync(command, ct);
        return SetResponse(result);
    }

    [HttpPost("consume")]
    [ProducesResponseType(typeof(ConsumeProviderEmailVerificationResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ConsumeProviderEmailVerificationResponse>> Consume(
        [FromBody] ConsumeProviderEmailVerificationRequest request, CancellationToken ct)
    {
        var command = new ConsumeProviderEmailVerificationCommand
        {
            Token = request.Token,
        };

        var result = await _sender.ProcessAsync(command, ct);
        return SetResponse(result);
    }

    [HttpPost("resend")]
    [ProducesResponseType(typeof(ResendProviderEmailVerificationResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ResendProviderEmailVerificationResponse>> Resend(
        [FromBody] ResendProviderEmailVerificationRequest request, CancellationToken ct)
    {
        var command = new ResendProviderEmailVerificationCommand
        {
            Email = request.Email,
        };

        var result = await _sender.ProcessAsync(command, ct);
        return SetResponse(result);
    }
}
