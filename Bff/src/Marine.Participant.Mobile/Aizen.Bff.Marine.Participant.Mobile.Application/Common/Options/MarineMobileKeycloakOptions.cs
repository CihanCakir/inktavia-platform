using System.ComponentModel.DataAnnotations;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Common.Options;

/// <summary>
/// Keycloak configuration for the Marine Participant Mobile BFF (full-IdP model).
/// Secrets must come from environment variables / secret store — never committed.
/// </summary>
public sealed class MarineMobileKeycloakOptions
{
    public const string SectionName = "MarineMobileKeycloak";

    /// <summary>Keycloak base URL, e.g. http://keycloak:8080 (no trailing slash required).</summary>
    [Required(ErrorMessage = "MarineMobileKeycloak:BaseUrl is required.")]
    public string BaseUrl { get; set; } = default!;

    /// <summary>Token issuer/authority. Defaults to {BaseUrl}/realms/{Realm} when omitted.</summary>
    public string? Authority { get; set; }

    /// <summary>OIDC metadata address (well-known). Optional; used for JWKS discovery when the browser-facing Authority differs from the in-cluster host.</summary>
    public string? MetadataAddress { get; set; }

    [Required(ErrorMessage = "MarineMobileKeycloak:Realm is required.")]
    public string Realm { get; set; } = default!;

    /// <summary>Expected audience of inbound participant access tokens. Defaults to BffClientId.</summary>
    public string? Audience { get; set; }

    public bool RequireHttpsMetadata { get; set; }

    /// <summary>Confidential client (service account) used for client_credentials + Keycloak Admin API.</summary>
    [Required(ErrorMessage = "MarineMobileKeycloak:AdminClientId is required.")]
    public string AdminClientId { get; set; } = default!;

    [Required(ErrorMessage = "MarineMobileKeycloak:AdminClientSecret is required.")]
    public string AdminClientSecret { get; set; } = default!;

    /// <summary>Public mobile SPA client id (used as client_id for redirect / verify flows).</summary>
    [Required(ErrorMessage = "MarineMobileKeycloak:ClientId is required.")]
    public string ClientId { get; set; } = default!;

    /// <summary>Resource server client id — expected audience of participant access tokens.</summary>
    [Required(ErrorMessage = "MarineMobileKeycloak:BffClientId is required.")]
    public string BffClientId { get; set; } = default!;

    /// <summary>Realm role marking an authenticated mobile participant.</summary>
    public string MobileUserRole { get; set; } = "mobile_user";

    /// <summary>Keycloak user attribute + token claim carrying the linked participant profile id.</summary>
    public string ParticipantProfileIdAttributeName { get; set; } = "participant_profile_id";

    /// <summary>Where Keycloak redirects the participant after the Verify Email action.</summary>
    public string? VerifyEmailRedirectUri { get; set; }

    /// <summary>
    /// Redirect URI the BFF presents to Keycloak during the native ticket→session handoff
    /// (server-side authorization-code + PKCE). It is never actually called back — the BFF reads the
    /// <c>code</c> out of the authorize 302 itself — but it must be a registered redirect on the
    /// <see cref="ClientId"/> (inktavia-mobile) client and identical in the authorize + token requests.
    /// </summary>
    public string BffRedirectUri { get; set; } = "http://localhost:17003/auth/callback";

    /// <summary>
    /// Shared secret sent to modules as X-Aizen-Bff-Assertion so they honor the BFF-asserted participant identity
    /// (must match each module's BffAssertion:SharedSecret). Empty = no assertion headers are sent.
    /// </summary>
    public string? ModuleAssertionSecret { get; set; }

    public string TokenEndpoint => $"{BaseUrl.TrimEnd('/')}/realms/{Realm}/protocol/openid-connect/token";

    public string AuthorizeEndpoint => $"{BaseUrl.TrimEnd('/')}/realms/{Realm}/protocol/openid-connect/auth";

    public string LogoutEndpoint => $"{BaseUrl.TrimEnd('/')}/realms/{Realm}/protocol/openid-connect/logout";

    public string AdminApiBaseUrl => $"{BaseUrl.TrimEnd('/')}/admin/realms/{Realm}";
}
