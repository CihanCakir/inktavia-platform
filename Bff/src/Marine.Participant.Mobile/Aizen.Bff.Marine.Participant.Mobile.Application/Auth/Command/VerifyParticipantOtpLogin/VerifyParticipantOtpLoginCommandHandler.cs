using System.Diagnostics;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Auth.OtpLogin;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Identity.Abstraction.Dto.OtpLogin;
using Microsoft.Extensions.Logging;
using Refit;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Auth;

/// <summary>
/// Identity verify (OTP → single-use login_ticket) then the native ticket→session handoff
/// (server-side auth-code + PKCE against inktavia-mobile) → real Keycloak tokens.
/// </summary>
public sealed class VerifyParticipantOtpLoginCommandHandler
    : AizenCommandHandler<VerifyParticipantOtpLoginCommand, MobileOtpVerifyResponse>
{
    private readonly IIdentityRemoteCall _identity;
    private readonly IParticipantSessionHandoff _handoff;
    private readonly ILogger<VerifyParticipantOtpLoginCommandHandler> _logger;

    public VerifyParticipantOtpLoginCommandHandler(
        IIdentityRemoteCall identity,
        IParticipantSessionHandoff handoff,
        ILogger<VerifyParticipantOtpLoginCommandHandler> logger)
    { _identity = identity; _handoff = handoff; _logger = logger; }

    public override async Task<MobileOtpVerifyResponse?> Handle(
        VerifyParticipantOtpLoginCommand request, CancellationToken ct)
    {
        string? loginTicket = null;
        try
        {
            var result = await _identity.VerifyParticipantOtpLogin(new VerifyProviderOtpLoginRequest
            { LoginRequestId = request.LoginRequestId, OtpCode = request.OtpCode });
            var data = result.Body;
            // A 2xx with Verified:false is a legitimate business outcome (wrong/expired code) — handled below
            // as a 400. Only genuine downstream/infra failures (non-2xx, timeout, transport) reach the catch.
            if (data is { Verified: true })
                loginTicket = data.LoginTicket;
        }
        catch (ApiException apiEx)
        {
            throw Upstream((int)apiEx.StatusCode, apiEx);
        }
        catch (Exception ex)
        {
            throw Upstream(null, ex);
        }

        // Wrong/expired code, or verified without a mintable ticket → business error (400, not 502).
        if (string.IsNullOrEmpty(loginTicket))
            throw new AizenBusinessException("The code is invalid or has expired. Please request a new code.");

        // Native ticket→session handoff: exchange the login_ticket for real Keycloak tokens.
        var tokens = await _handoff.ExchangeAsync(loginTicket, ct);

        return new MobileOtpVerifyResponse
        {
            AccessToken = tokens.AccessToken,
            RefreshToken = tokens.RefreshToken,
            ExpiresIn = tokens.ExpiresIn,
            TokenType = tokens.TokenType,
        };
    }

    private AizenUpstreamException Upstream(int? status, Exception? ex)
    {
        var correlationId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
        _logger.LogError(ex,
            "[{Code}] Participant OTP login verify failed (upstream=Identity status={Status}). correlationId={CorrelationId}",
            AizenUpstreamException.StableCode, status, correlationId);
        return new AizenUpstreamException(correlationId, status);
    }
}
