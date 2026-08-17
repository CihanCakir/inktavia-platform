using System.Diagnostics;
using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Contracts.Auth.OtpLogin;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Identity.Abstraction.Dto.OtpLogin;
using Microsoft.Extensions.Logging;
using Refit;

namespace Aizen.Bff.MarineProvider.Application.Auth;

public sealed class VerifyOtpLoginCommandHandler
    : AizenCommandHandler<VerifyOtpLoginCommand, OtpLoginVerifyResponse>
{
    private readonly IIdentityRemoteCall _identity;
    private readonly ILogger<VerifyOtpLoginCommandHandler> _logger;

    public VerifyOtpLoginCommandHandler(IIdentityRemoteCall identity, ILogger<VerifyOtpLoginCommandHandler> logger)
    { _identity = identity; _logger = logger; }

    public override async Task<OtpLoginVerifyResponse?> Handle(VerifyOtpLoginCommand request, CancellationToken ct)
    {
        try
        {
            var result = await _identity.VerifyProviderOtpLogin(new VerifyProviderOtpLoginRequest
            { LoginRequestId = request.LoginRequestId, OtpCode = request.OtpCode });
            var data = result.Body;
            if (data is not null)
                return new OtpLoginVerifyResponse
                {
                    Verified = data.Verified, NextAction = data.NextAction,
                    LoginTicket = data.LoginTicket,
                    ExpiresInSeconds = data.ExpiresInSeconds, Message = data.Message,
                };
        }
        catch (ApiException apiEx)
        {
            // Downstream/infra failure (403/5xx/timeout) — surface, do NOT mask as a business Verified:false.
            throw Upstream((int)apiEx.StatusCode, apiEx);
        }
        catch (Exception ex)
        {
            throw Upstream(null, ex);
        }

        // 2xx with a null body: no ticket minted → business "verification failed" (a real Verified:false on a
        // 2xx is already returned above). Not an infra failure, so it stays a uniform business result.
        return new OtpLoginVerifyResponse { Verified = false, NextAction = "keycloak_handoff_required", Message = "Verification failed." };
    }

    private AizenUpstreamException Upstream(int? status, Exception? ex)
    {
        var correlationId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
        _logger.LogError(ex,
            "[{Code}] Provider OTP login verify failed (upstream=Identity status={Status}). correlationId={CorrelationId}",
            AizenUpstreamException.StableCode, status, correlationId);
        return new AizenUpstreamException(correlationId, status);
    }
}
