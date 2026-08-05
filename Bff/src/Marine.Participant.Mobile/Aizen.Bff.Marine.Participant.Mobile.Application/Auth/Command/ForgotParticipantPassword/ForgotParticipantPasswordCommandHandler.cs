using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Auth.Password;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto.PasswordRecovery;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Auth;

/// <summary>
/// Start participant password recovery. The channel (email/phone) is derived from the identifier shape,
/// mirroring the OTP-login send endpoint. Anti-enumeration: always returns a neutral 200 shape — Identity
/// mints a synthetic resetRequestId even for unknown identifiers, so the client flow is identical either way.
/// </summary>
public sealed class ForgotParticipantPasswordCommandHandler
    : AizenCommandHandler<ForgotParticipantPasswordCommand, MobileForgotPasswordResponse>
{
    private readonly IIdentityRemoteCall _identity;
    private readonly ILogger<ForgotParticipantPasswordCommandHandler> _logger;

    public ForgotParticipantPasswordCommandHandler(
        IIdentityRemoteCall identity, ILogger<ForgotParticipantPasswordCommandHandler> logger)
    { _identity = identity; _logger = logger; }

    public override async Task<MobileForgotPasswordResponse?> Handle(
        ForgotParticipantPasswordCommand request, CancellationToken ct)
    {
        var identifier = (request.Identifier ?? string.Empty).Trim();
        var channel = identifier.Contains('@') ? "email" : "phone";

        try
        {
            var result = await _identity.RequestParticipantPasswordRecovery(new RequestProviderPasswordRecoveryRequest
            { Channel = channel, Identifier = identifier });
            var data = result.Body;
            if (data is not null)
                return new MobileForgotPasswordResponse
                {
                    ResetRequestId = data.ResetRequestId,
                    MaskedTarget = data.MaskedTarget,
                    ExpiresInSeconds = data.ExpiresInSeconds,
                };
        }
        catch (Exception ex)
        {
            // Anti-enumeration + resilience: never surface which identifiers exist or that Identity hiccuped.
            _logger.LogError(ex, "Participant password recovery request failed.");
        }

        // Identity unreachable or null body — return a neutral shape (Identity always mints a synthetic id,
        // so a real request never reaches here empty; this is the defensive fallback only).
        return new MobileForgotPasswordResponse();
    }
}
