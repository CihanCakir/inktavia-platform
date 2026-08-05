using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Auth;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Auth;

public sealed class RefreshParticipantCommandHandler
    : AizenCommandHandler<RefreshParticipantCommand, MobileAuthTokenResponse>
{
    private readonly IParticipantKeycloakAuthClient _auth;
    public RefreshParticipantCommandHandler(IParticipantKeycloakAuthClient auth) => _auth = auth;

    public override async Task<MobileAuthTokenResponse?> Handle(
        RefreshParticipantCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return null; // → 401

        var tokens = await _auth.RefreshAsync(request.RefreshToken, ct);
        if (tokens is null)
            return null; // invalid/expired → 401

        return new MobileAuthTokenResponse
        {
            AccessToken = tokens.AccessToken,
            RefreshToken = tokens.RefreshToken,
            ExpiresIn = tokens.ExpiresIn,
            TokenType = tokens.TokenType,
        };
    }
}
