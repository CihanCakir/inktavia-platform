using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Auth.OtpLogin;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto.OtpLogin;
using Microsoft.Extensions.Logging;

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
        try
        {
            var result = await _identity.RequestParticipantOtpLogin(new RequestProviderOtpLoginRequest
            { Channel = request.Channel, Identifier = request.Identifier });
            var data = result.Body;
            if (data is not null)
                return new MobileOtpSendResponse
                {
                    LoginRequestId = data.LoginRequestId,
                    MaskedTarget = data.MaskedTarget,
                    ExpiresInSeconds = data.ExpiresInSeconds,
                };
        }
        catch (Exception ex)
        {
            // Anti-enumeration + resilience: never surface which identifiers exist or that Identity hiccuped.
            _logger.LogError(ex, "Participant OTP login request failed.");
        }

        // Identity unreachable or null body — return a neutral shape (Identity always mints a synthetic id,
        // so a real request never reaches here empty; this is the defensive fallback only).
        return new MobileOtpSendResponse();
    }
}
