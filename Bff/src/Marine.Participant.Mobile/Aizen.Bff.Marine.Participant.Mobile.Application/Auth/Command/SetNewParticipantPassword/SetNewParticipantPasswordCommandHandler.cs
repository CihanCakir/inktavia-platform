using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Auth.Password;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Identity.Abstraction.Dto.PasswordRecovery;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Auth;

/// <summary>
/// Complete recovery: exchange the single-use reset token for a new Keycloak password (Identity resets it and
/// best-effort revokes sessions). A reused/expired token, a mismatched confirmation, or any Identity-side
/// failure surfaces as a clean business error (400).
/// </summary>
public sealed class SetNewParticipantPasswordCommandHandler
    : AizenCommandHandler<SetNewParticipantPasswordCommand, MobileSetNewPasswordResponse>
{
    private readonly IIdentityRemoteCall _identity;
    private readonly ILogger<SetNewParticipantPasswordCommandHandler> _logger;

    public SetNewParticipantPasswordCommandHandler(
        IIdentityRemoteCall identity, ILogger<SetNewParticipantPasswordCommandHandler> logger)
    { _identity = identity; _logger = logger; }

    public override async Task<MobileSetNewPasswordResponse?> Handle(
        SetNewParticipantPasswordCommand request, CancellationToken ct)
    {
        // Fail fast on a client-side mismatch (Identity also enforces this) — clearer message, no round-trip.
        if (!string.Equals(request.NewPassword, request.ConfirmPassword, StringComparison.Ordinal))
            throw new AizenBusinessException("Passwords do not match.");

        ResetProviderPasswordResponse? data;
        try
        {
            var result = await _identity.ResetParticipantPassword(new ResetProviderPasswordRequest
            {
                ResetToken = request.ResetToken,
                NewPassword = request.NewPassword,
                ConfirmPassword = request.ConfirmPassword,
            });
            data = result.Body;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Participant set-new-password (Identity call) failed.");
            throw new AizenBusinessException("Your password could not be reset. Please try again.");
        }

        if (data is not { Success: true })
            throw new AizenBusinessException(
                string.IsNullOrWhiteSpace(data?.Message)
                    ? "Your reset session has expired. Please restart the password recovery."
                    : data.Message);

        return new MobileSetNewPasswordResponse { Success = true };
    }
}
