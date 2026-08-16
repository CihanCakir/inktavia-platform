using System.Diagnostics;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Auth.OtpLogin;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Identity.Abstraction.Dto.OtpLogin;
using Microsoft.Extensions.Logging;
using Refit;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Auth;

public sealed class RequestParticipantOtpLoginCommandHandler
    : AizenCommandHandler<RequestParticipantOtpLoginCommand, MobileOtpSendResponse>
{
    private readonly IIdentityRemoteCall _identity;
    private readonly ILogger<RequestParticipantOtpLoginCommandHandler> _logger;

    public RequestParticipantOtpLoginCommandHandler(
        IIdentityRemoteCall identity, ILogger<RequestParticipantOtpLoginCommandHandler> logger)
    { _identity = identity; _logger = logger; }

    public override async Task<MobileOtpSendResponse?> Handle(
        RequestParticipantOtpLoginCommand request, CancellationToken ct)
    {
        // Anti-enumeration is done INSIDE Identity: RequestParticipantOtpLogin always returns 200 with a
        // (real or synthetic) LoginRequestId regardless of whether the account exists. So a 2xx body is the
        // safe uniform response and we pass it straight through. Only genuine downstream/infra failures
        // (403 stripped role, 5xx, timeout, unparseable/empty body) get here as an error — those must SURFACE
        // as 502, never be masked into a 200 with an empty loginRequestId (the bug this fixes).
        MobileOtpSendResponse? passthrough;
        try
        {
            var result = await _identity.RequestParticipantOtpLogin(new RequestProviderOtpLoginRequest
            { Channel = request.Channel, Identifier = request.Identifier });
            var data = result.Body;
            passthrough = data is not null
                ? new MobileOtpSendResponse
                {
                    LoginRequestId = data.LoginRequestId,
                    MaskedTarget = data.MaskedTarget,
                    ExpiresInSeconds = data.ExpiresInSeconds,
                }
                : null;
        }
        catch (ApiException apiEx)
        {
            throw Upstream((int)apiEx.StatusCode, apiEx);
        }
        catch (Exception ex)
        {
            throw Upstream(null, ex);
        }

        // 2xx but empty/unparseable body — never legitimate here (Identity always mints an id) → surface.
        if (passthrough is null || string.IsNullOrEmpty(passthrough.LoginRequestId))
            throw Upstream(null, null, "empty body / blank loginRequestId");

        return passthrough;
    }

    private AizenUpstreamException Upstream(int? status, Exception? ex, string? note = null)
    {
        var correlationId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
        _logger.LogError(ex,
            "[{Code}] Participant OTP login request failed (upstream=Identity status={Status} {Note}). correlationId={CorrelationId}",
            AizenUpstreamException.StableCode, status, note, correlationId);
        return new AizenUpstreamException(correlationId, status);
    }
}
