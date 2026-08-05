using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Auth.Password;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Identity.Abstraction.Dto.PasswordRecovery;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Auth;

/// <summary>
/// Verify the recovery OTP → single-use reset token. A wrong/expired code (or a verified response without a
/// mintable token) is a clean business error (400), never a 500. The reset token is opaque
/// (<c>{resetRequestId}.{secret}</c>) and only usable once, at set-new-password.
/// </summary>
public sealed class VerifyParticipantPasswordOtpCommandHandler
    : AizenCommandHandler<VerifyParticipantPasswordOtpCommand, MobileVerifyPasswordOtpResponse>
{
    private readonly IIdentityRemoteCall _identity;
    private readonly ILogger<VerifyParticipantPasswordOtpCommandHandler> _logger;

    public VerifyParticipantPasswordOtpCommandHandler(
        IIdentityRemoteCall identity, ILogger<VerifyParticipantPasswordOtpCommandHandler> logger)
    { _identity = identity; _logger = logger; }

    public override async Task<MobileVerifyPasswordOtpResponse?> Handle(
        VerifyParticipantPasswordOtpCommand request, CancellationToken ct)
    {
        string? resetToken = null;
        try
        {
            var result = await _identity.VerifyParticipantPasswordRecoveryOtp(new VerifyProviderPasswordRecoveryOtpRequest
            { ResetRequestId = request.ResetRequestId, OtpCode = request.OtpCode });
            var data = result.Body;
            if (data is { Verified: true })
                resetToken = data.ResetToken;
        }
        catch (Exception ex)
        {
            // Identity/transport failure — clean business error, no internals leaked.
            _logger.LogError(ex, "Participant password recovery verify (Identity call) failed.");
            throw new AizenBusinessException("The code could not be verified. Please try again.");
        }

        if (string.IsNullOrEmpty(resetToken))
            throw new AizenBusinessException("The code is invalid or has expired. Please request a new code.");

        return new MobileVerifyPasswordOtpResponse { ResetToken = resetToken };
    }
}
