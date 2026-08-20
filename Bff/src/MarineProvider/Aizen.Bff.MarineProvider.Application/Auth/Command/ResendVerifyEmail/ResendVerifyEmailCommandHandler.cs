using System.Diagnostics;
using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Contracts.Auth.EmailVerification;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Identity.Abstraction.Dto.EmailVerification;
using Microsoft.Extensions.Logging;
using Refit;

namespace Aizen.Bff.MarineProvider.Application.Auth;

/// <summary>
/// Doğrulama e-postasını yeniden gönderir. Numaralandırma korumalı: Identity her zaman aynı genel yanıtı döner
/// (hesap olsa da olmasa da), BFF da onu aynen iletir. Gerçek downstream hatası (5xx/timeout) → 502, asla 200 maskesi.
/// </summary>
public sealed class ResendVerifyEmailCommandHandler
    : AizenCommandHandler<ResendVerifyEmailCommand, ResendVerifyEmailResponse>
{
    private readonly IIdentityRemoteCall _identity;
    private readonly ILogger<ResendVerifyEmailCommandHandler> _logger;

    public ResendVerifyEmailCommandHandler(IIdentityRemoteCall identity, ILogger<ResendVerifyEmailCommandHandler> logger)
    {
        _identity = identity;
        _logger = logger;
    }

    public override async Task<ResendVerifyEmailResponse?> Handle(ResendVerifyEmailCommand request, CancellationToken ct)
    {
        try
        {
            var result = await _identity.ResendProviderEmailVerification(
                new ResendProviderEmailVerificationRequest { Email = request.Email });
            var data = result.Body;
            if (data is not null)
                return new ResendVerifyEmailResponse
                {
                    Accepted = data.Accepted,
                    MaskedTarget = data.MaskedTarget,
                    ResendAfterSeconds = data.ResendAfterSeconds,
                    ExpiresInSeconds = data.ExpiresInSeconds,
                    Message = data.Message,
                };
        }
        catch (ApiException apiEx) { throw Upstream((int)apiEx.StatusCode, apiEx); }
        catch (Exception ex) { throw Upstream(null, ex); }

        // 2xx + null body → numaralandırma koruması: yine de aynı genel yanıtı ver.
        return new ResendVerifyEmailResponse
        {
            Accepted = true,
            Message = "Hesabınız varsa ve e-postanız henüz doğrulanmadıysa yeni bir doğrulama bağlantısı gönderildi.",
        };
    }

    private AizenUpstreamException Upstream(int? status, Exception? ex)
    {
        var correlationId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
        _logger.LogError(ex,
            "[{Code}] Provider email verify resend failed (upstream=Identity status={Status}). correlationId={CorrelationId}",
            AizenUpstreamException.StableCode, status, correlationId);
        return new AizenUpstreamException(correlationId, status);
    }
}
