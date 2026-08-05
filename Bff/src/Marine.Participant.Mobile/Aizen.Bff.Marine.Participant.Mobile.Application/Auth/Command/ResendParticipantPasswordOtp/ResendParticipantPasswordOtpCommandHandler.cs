using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Auth.Password;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto.PasswordRecovery;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Auth;

/// <summary>
/// Resend the recovery OTP for an in-flight resetRequestId (honors the Identity cooldown). Anti-enumeration:
/// returns a neutral cooldown shape regardless of whether the request id exists.
/// </summary>
public sealed class ResendParticipantPasswordOtpCommandHandler
    : AizenCommandHandler<ResendParticipantPasswordOtpCommand, MobileResendPasswordOtpResponse>
{
    private readonly IIdentityRemoteCall _identity;
    private readonly ILogger<ResendParticipantPasswordOtpCommandHandler> _logger;

    public ResendParticipantPasswordOtpCommandHandler(
        IIdentityRemoteCall identity, ILogger<ResendParticipantPasswordOtpCommandHandler> logger)
    { _identity = identity; _logger = logger; }

    public override async Task<MobileResendPasswordOtpResponse?> Handle(
        ResendParticipantPasswordOtpCommand request, CancellationToken ct)
    {
        try
        {
            var result = await _identity.ResendParticipantPasswordRecoveryOtp(new ResendProviderPasswordRecoveryOtpRequest
            { ResetRequestId = request.ResetRequestId });
            var data = result.Body;
            if (data is not null)
                return new MobileResendPasswordOtpResponse
                {
                    ResendAfterSeconds = data.ResendAfterSeconds,
                    ExpiresInSeconds = data.ExpiresInSeconds,
                };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Participant password recovery resend failed.");
        }

        return new MobileResendPasswordOtpResponse();
    }
}
