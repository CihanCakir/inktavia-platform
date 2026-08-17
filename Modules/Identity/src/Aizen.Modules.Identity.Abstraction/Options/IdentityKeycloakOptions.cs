namespace Aizen.Modules.Identity.Abstraction.Options;

/// <summary>
/// Identity-side Keycloak Admin API configuration for provider role synchronization
/// (approval/suspend → Keycloak realm roles). Secrets come from env/secret store — never hardcoded.
/// When <see cref="Enabled"/> is false or BaseUrl is empty, role sync is a no-op (safe for envs without Keycloak).
/// </summary>
public sealed class IdentityKeycloakOptions
{
    public const string SectionName = "IdentityKeycloak";

    public bool Enabled { get; set; } = true;

    public string BaseUrl { get; set; } = string.Empty;
    public string Realm { get; set; } = string.Empty;
    public string AdminClientId { get; set; } = string.Empty;
    public string AdminClientSecret { get; set; } = string.Empty;

    public string ProviderPendingRole { get; set; } = "provider_pending";
    public string ProviderUserRole { get; set; } = "provider_user";
    public string ProviderRestrictedRole { get; set; } = "provider_restricted";

    public bool RevokeSessionsOnSuspend { get; set; }

    public string TokenEndpoint => $"{BaseUrl.TrimEnd('/')}/realms/{Realm}/protocol/openid-connect/token";
    public string AdminApiBaseUrl => $"{BaseUrl.TrimEnd('/')}/admin/realms/{Realm}";
}
