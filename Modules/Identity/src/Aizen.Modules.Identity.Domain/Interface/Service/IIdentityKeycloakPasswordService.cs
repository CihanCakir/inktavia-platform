namespace Aizen.Modules.Identity.Domain.Interface.Service;

public interface IIdentityKeycloakPasswordService
{
    /// <summary>Sets a new permanent (non-temporary) password for the Keycloak user via the Admin API.</summary>
    Task ResetPasswordAsync(string keycloakUserId, string newPassword, CancellationToken ct);

    /// <summary>Revokes the user's active Keycloak sessions (recommended after a password reset).</summary>
    Task RevokeSessionsAsync(string keycloakUserId, CancellationToken ct);
}
