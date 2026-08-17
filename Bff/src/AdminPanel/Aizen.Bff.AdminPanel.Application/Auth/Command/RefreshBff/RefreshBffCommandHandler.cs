using Aizen.Bff.AdminPanel.Application.Common.Services;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Identity.Abstraction.Response;

namespace Aizen.Bff.AdminPanel.Application.Auth.Command;

[DocumentationInfo("Refresh command handler",
    "Refreshes the admin session against Keycloak (refresh_token grant on the public admin-panel client) — the issuer " +
    "of the admin's Keycloak session. Replaces the legacy Identity RefreshLogin (UserLoginTokenEntity store), which " +
    "never held these Keycloak tokens (→ TokenNotFound). Invalid/expired refresh → RefreshTokenTimeOut fail envelope.")]
public sealed class RefreshCommandHandler : AizenCommandHandler<RefreshBffCommand, UserLoginResponse>
{
    // AizenErrorCode.RefreshTokenTimeOut — surfaced as a fail envelope so admin-web routes to /login exactly once.
    private const int RefreshTokenTimeOut = 40102;

    private readonly IAdminKeycloakAuthClient _keycloak;

    public RefreshCommandHandler(IAdminKeycloakAuthClient keycloak)
    {
        _keycloak = keycloak;
    }

    public override async Task<UserLoginResponse?> Handle(RefreshBffCommand request, CancellationToken ct)
    {
        var tokens = await _keycloak.RefreshAsync(request.Request.RefreshToken, ct);
        if (tokens is null)
            throw new AizenBusinessException(RefreshTokenTimeOut);

        var now = DateTime.UtcNow;
        var (email, givenName, familyName) = AdminJwtReader.ReadProfile(tokens.AccessToken);

        // Shape the Keycloak token set into the UserLoginResponse the admin-web already parses
        // (body.token.{accessToken,refreshToken,accessTokenExpiredDate}). Profile is derived from the access-token
        // claims; the FE re-derives the user from the token itself, so a minimal profile is sufficient here.
        return new UserLoginResponse
        {
            Token = new TokenInfo(
                tokens.AccessToken,
                now.AddSeconds(tokens.ExpiresIn),
                tokens.RefreshToken,
                AdminJwtReader.ReadExpiryUtc(tokens.RefreshToken) ?? now),
            Profile = new UserInfo(
                UserId: 0,
                Email: email,
                NationalityId: null,
                Name: givenName,
                Surname: familyName,
                PhoneNumber: null),
            Agreement = new AgreementInfo(),
        };
    }
}
