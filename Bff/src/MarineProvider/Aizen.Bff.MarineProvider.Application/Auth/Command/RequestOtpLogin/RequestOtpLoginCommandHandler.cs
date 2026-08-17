using System.Diagnostics;
using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Contracts.Auth.OtpLogin;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Identity.Abstraction.Dto.OtpLogin;
using Microsoft.Extensions.Logging;
using Refit;

namespace Aizen.Bff.MarineProvider.Application.Auth;

public sealed class RequestOtpLoginCommandHandler
    : AizenCommandHandler<RequestOtpLoginCommand, OtpLoginRequestResponse>
{
    private readonly IIdentityRemoteCall _identity;
    private readonly ILogger<RequestOtpLoginCommandHandler> _logger;

    public RequestOtpLoginCommandHandler(IIdentityRemoteCall identity, ILogger<RequestOtpLoginCommandHandler> logger)
    { _identity = identity; _logger = logger; }

    public override async Task<OtpLoginRequestResponse?> Handle(RequestOtpLoginCommand request, CancellationToken ct)
    {
        try
        {
            var result = await _identity.RequestProviderOtpLogin(new RequestProviderOtpLoginRequest
            { Channel = request.Channel, Identifier = request.Identifier });
            var data = result.Body;
            if (data is not null)
                return new OtpLoginRequestResponse
                {
                    Accepted = data.Accepted, LoginRequestId = data.LoginRequestId,
                    MaskedTarget = data.MaskedTarget, OtpLength = data.OtpLength,
                    ExpiresInSeconds = data.ExpiresInSeconds, ResendAfterSeconds = data.ResendAfterSeconds,
                    Message = data.Message,
                };
        }
        catch (ApiException apiEx)
        {
            // Genuine downstream failure (403/5xx/timeout) — surface as 502, never mask as a 200.
            throw Upstream((int)apiEx.StatusCode, apiEx);
        }
        catch (Exception ex)
        {
            throw Upstream(null, ex);
        }

        // 2xx with a null body → unknown account (anti-enumeration): KEEP the uniform "code sent" 200.
        return new OtpLoginRequestResponse { Accepted = true, Message = "If an account exists, a verification code has been sent." };
    }

    private AizenUpstreamException Upstream(int? status, Exception? ex)
    {
        var correlationId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
        _logger.LogError(ex,
            "[{Code}] Provider OTP login request failed (upstream=Identity status={Status}). correlationId={CorrelationId}",
            AizenUpstreamException.StableCode, status, correlationId);
        return new AizenUpstreamException(correlationId, status);
    }
}
