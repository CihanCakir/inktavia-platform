using System.ComponentModel.DataAnnotations;

namespace Aizen.Bff.Marine.Web.Application.Common.Options;

/// <summary>
/// Keycloak configuration for the Marine Web BFF (the public MarineOS website's BFF).
/// Secrets must come from environment variables / secret store — never committed.
/// Mirrors <c>MarineMobileKeycloakOptions</c>, web-flavored (role default <c>web_user</c>).
/// </summary>
public sealed class MarineWebKeycloakOptions
{
    public const string SectionName = "MarineWebKeycloak";

    /// <summary>Keycloak base URL, e.g. http://keycloak:8080 (no trailing slash required).</summary>
    [Required(ErrorMessage = "MarineWebKeycloak:BaseUrl is required.")]
    public string BaseUrl { get; set; } = default!;

    /// <summary>Token issuer/authority. Defaults to {BaseUrl}/realms/{Realm} when omitted.</summary>
    public string? Authority { get; set; }

    /// <summary>OIDC metadata address (well-known). Optional; used for JWKS discovery when the browser-facing Authority differs from the in-cluster host.</summary>
    public string? MetadataAddress { get; set; }

    [Required(ErrorMessage = "MarineWebKeycloak:Realm is required.")]
    public string Realm { get; set; } = default!;

    /// <summary>Expected audience of inbound participant access tokens. Defaults to BffClientId.</summary>
    public string? Audience { get; set; }

    public bool RequireHttpsMetadata { get; set; }

    /// <summary>Confidential client (service account) used for client_credentials + Keycloak Admin API.</summary>
    [Required(ErrorMessage = "MarineWebKeycloak:AdminClientId is required.")]
    public string AdminClientId { get; set; } = default!;

    [Required(ErrorMessage = "MarineWebKeycloak:AdminClientSecret is required.")]
    public string AdminClientSecret { get; set; } = default!;

    /// <summary>Public web SPA client id (used as client_id for redirect / verify flows).</summary>
    [Required(ErrorMessage = "MarineWebKeycloak:ClientId is required.")]
    public string ClientId { get; set; } = default!;

    /// <summary>Resource server client id — expected audience of participant access tokens.</summary>
    [Required(ErrorMessage = "MarineWebKeycloak:BffClientId is required.")]
    public string BffClientId { get; set; } = default!;

    /// <summary>Realm role marking an authenticated web participant.</summary>
    public string WebUserRole { get; set; } = "web_user";

    /// <summary>Keycloak user attribute + token claim carrying the linked participant profile id.</summary>
    public string ParticipantProfileIdAttributeName { get; set; } = "participant_profile_id";

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
