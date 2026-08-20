using System.Diagnostics;
using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Bff.MarineProvider.Application.Contracts.Auth.EmailVerification;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Identity.Abstraction.Dto.EmailVerification;
using Microsoft.Extensions.Logging;
using Refit;

namespace Aizen.Bff.MarineProvider.Application.Auth;

/// <summary>
/// E-posta doğrulama onayı. SIRA KRİTİK: önce bizim DB'de onayla (Identity confirm → EmailConfirmed=true), SONRA
/// Keycloak'ta emailVerified=true. Keycloak bizim onaydan SONRA patlarsa 502 döneriz (verified=true DEĞİL) —
/// yerleşik token idempotent olduğundan kullanıcı aynı linke yeniden tıklar ve iki sistem de senkronlanır.
/// Böylece "biri true diğeri false" ayrışması oluşmaz. Süresi dolmuş vs geçersiz ayrımı korunur (FE dallanır).
/// </summary>
public sealed class VerifyEmailCommandHandler
    : AizenCommandHandler<VerifyEmailCommand, VerifyEmailResponse>
{
    private readonly IIdentityRemoteCall _identity;
    private readonly IProviderKeycloakAdminClient _keycloak;
    private readonly ILogger<VerifyEmailCommandHandler> _logger;

    public VerifyEmailCommandHandler(
        IIdentityRemoteCall identity,
        IProviderKeycloakAdminClient keycloak,
        ILogger<VerifyEmailCommandHandler> logger)
    {
        _identity = identity;
        _keycloak = keycloak;
        _logger = logger;
    }

    public override async Task<VerifyEmailResponse?> Handle(VerifyEmailCommand request, CancellationToken ct)
    {
        // 1) Bizim DB'de onayla (Identity). Genuine downstream hatası → 502.
        ConfirmProviderEmailVerificationResponse? body;
        try
        {
            var confirm = await _identity.ConfirmProviderEmailVerification(
                new ConfirmProviderEmailVerificationRequest { Token = request.Token });
            body = confirm.Body;
        }
        catch (ApiException apiEx) { throw Upstream((int)apiEx.StatusCode, apiEx, "identity.confirm"); }
        catch (Exception ex) { throw Upstream(null, ex, "identity.confirm"); }

        if (body is null) throw Upstream(null, null, "identity.confirm: null body");

        // 2) Onaylanmadıysa (süresi dolmuş/geçersiz) — 200, istemci durumu. Sızıntı yok: tıklayan zaten token'a sahip.
        if (!body.Confirmed)
            return new VerifyEmailResponse { Verified = false, Status = body.Status, Message = body.Message };

        // Onaylandı ama subject yoksa tamamlanamaz (veri tutarsızlığı) → 502.
        if (string.IsNullOrWhiteSpace(body.KeycloakSubjectId))
            throw Upstream(null, null, "identity.confirm: confirmed but missing keycloak subject");

        // 3) EN SON Keycloak. Buradaki hata bizim onaydan SONRA olur → 502, kullanıcı linke yeniden tıklar (idempotent).
        try
        {
            await _keycloak.SetEmailVerifiedAsync(body.KeycloakSubjectId!, ct);
        }
        catch (Exception kcEx) { throw Upstream(null, kcEx, "keycloak.setEmailVerified"); }

        return new VerifyEmailResponse
        {
            Verified = true,
            Status = "Confirmed",
            Message = "E-posta adresiniz doğrulandı.",
        };
    }

    private AizenUpstreamException Upstream(int? status, Exception? ex, string step)
    {
        var correlationId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
        _logger.LogError(ex,
            "[{Code}] Provider email verify failed at {Step} (status={Status}). correlationId={CorrelationId}",
            AizenUpstreamException.StableCode, step, status, correlationId);
        return new AizenUpstreamException(correlationId, status);
    }
}
