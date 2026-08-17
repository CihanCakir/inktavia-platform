using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Auth;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Auth;

public sealed class LogoutParticipantCommandHandler
    : AizenCommandHandler<LogoutParticipantCommand, MobileLogoutResponse>
{
    private readonly IParticipantKeycloakAuthClient _auth;
    public LogoutParticipantCommandHandler(IParticipantKeycloakAuthClient auth) => _auth = auth;

    public override async Task<MobileLogoutResponse?> Handle(
        LogoutParticipantCommand request, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(request.RefreshToken))
            await _auth.LogoutAsync(request.RefreshToken, ct);

        // Idempotent from the app's perspective — always report success.
        return new MobileLogoutResponse { Success = true };
    }
}
