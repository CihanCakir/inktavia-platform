using System.ComponentModel.DataAnnotations;

namespace Aizen.Bff.MarineProvider.Application.Common.Options;

/// <summary>
/// Keycloak configuration for the MarineProvider BFF (full-IdP model).
/// Secrets must come from environment variables / secret store — never committed.
/// </summary>
public sealed class MarineProviderKeycloakOptions
{
    public const string SectionName = "MarineProviderKeycloak";

    /// <summary>Keycloak base URL, e.g. http://keycloak:8080 (no trailing slash required).</summary>
    [Required(ErrorMessage = "MarineProviderKeycloak:BaseUrl is required.")]
    public string BaseUrl { get; set; } = default!;

    /// <summary>Token issuer/authority. Defaults to {BaseUrl}/realms/{Realm} when omitted.</summary>
    public string? Authority { get; set; }

    /// <summary>OIDC metadata address (well-known). Optional; used for JWKS discovery when the browser-facing Authority differs from the in-cluster host.</summary>
    public string? MetadataAddress { get; set; }

    [Required(ErrorMessage = "MarineProviderKeycloak:Realm is required.")]
    public string Realm { get; set; } = default!;

    /// <summary>Expected audience of inbound provider access tokens. Defaults to ProviderPortalBffClientId.</summary>
    public string? Audience { get; set; }

    public bool RequireHttpsMetadata { get; set; }

    /// <summary>Confidential client (service account) used for client_credentials + Keycloak Admin API.</summary>
    [Required(ErrorMessage = "MarineProviderKeycloak:AdminClientId is required.")]
    public string AdminClientId { get; set; } = default!;

    [Required(ErrorMessage = "MarineProviderKeycloak:AdminClientSecret is required.")]
    public string AdminClientSecret { get; set; } = default!;

    /// <summary>Public SPA client id (used as client_id for Verify Email / redirect flows).</summary>
    [Required(ErrorMessage = "MarineProviderKeycloak:ProviderPortalClientId is required.")]
    public string ProviderPortalClientId { get; set; } = default!;

    /// <summary>Resource server client id — expected audience of provider access tokens.</summary>
    [Required(ErrorMessage = "MarineProviderKeycloak:ProviderPortalBffClientId is required.")]
    public string ProviderPortalBffClientId { get; set; } = default!;

    public string ProviderPendingRole { get; set; } = "provider_pending";
    public string ProviderUserRole { get; set; } = "provider_user";
    public string ProviderRestrictedRole { get; set; } = "provider_restricted";

    /// <summary>Keycloak user attribute + token claim carrying the linked Organizer profile id.</summary>
    public string ProviderProfileIdAttributeName { get; set; } = "provider_profile_id";

    /// <summary>
    /// Shared secret sent to modules as X-Aizen-Bff-Assertion so they honor the BFF-asserted provider identity
    /// (must match each module's BffAssertion:SharedSecret). Empty = no assertion headers are sent.
    /// </summary>
    public string? ModuleAssertionSecret { get; set; }

    public string TokenEndpoint => $"{BaseUrl.TrimEnd('/')}/realms/{Realm}/protocol/openid-connect/token";

    public string AdminApiBaseUrl => $"{BaseUrl.TrimEnd('/')}/admin/realms/{Realm}";
}
