using Aizen.Core.InfoAccessor.Abstraction;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace Aizen.Core.InfoAccessor;

/// <summary>
/// Injects Identity token roles from <see cref="AizenUserInfo"/> into the active <see cref="ClaimsPrincipal"/>
/// after service token authentication (Keycloak) completes. This is required because internal module APIs
/// receive the Keycloak service token in the <c>Authorization</c> header (for BFF identity), while the
/// real user roles are carried in the Identity token parsed from <c>X-Aizen-User-Token</c> by
/// <see cref="Middlewares.AizenUserInfoMiddleware"/> which runs before authentication. Without this
/// transformation, <c>[Authorize(Roles = ...)]</c> would check the Keycloak service account principal
/// and never find application-level roles such as &quot;Admin&quot;.
/// </summary>
internal sealed class AizenIdentityClaimsTransformation : IClaimsTransformation
{
    private readonly IAizenInfoAccessor _infoAccessor;

    public AizenIdentityClaimsTransformation(IAizenInfoAccessor infoAccessor)
    {
        _infoAccessor = infoAccessor;
    }

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var userInfo = _infoAccessor.UserInfoAccessor?.UserInfo;
        if (userInfo?.Roles?.Any() != true)
            return Task.FromResult(principal);

        var identity = new ClaimsIdentity(
            userInfo.Roles.Select(r => new Claim(ClaimTypes.Role, r)),
            "AizenIdentityToken");

        principal.AddIdentity(identity);
        return Task.FromResult(principal);
    }
}
