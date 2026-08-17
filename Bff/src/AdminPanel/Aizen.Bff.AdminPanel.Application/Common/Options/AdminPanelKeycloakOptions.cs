namespace Aizen.Bff.AdminPanel.Application.Common.Options;

/// <summary>
/// AdminPanel BFF Keycloak options. Mirrors the MarineProvider BFF's <c>MarineProviderKeycloakOptions</c> for the
/// members the outbound delegating handler needs — the inbound auth extension reads the RS256/JWKS members directly
/// from configuration. Bound from the <c>AdminPanelKeycloak</c> section.
/// </summary>
public sealed class AdminPanelKeycloakOptions
{
    public const string SectionName = "AdminPanelKeycloak";

    /// <summary>
    /// Shared secret sent to modules as <c>X-Aizen-Bff-Assertion</c> so they honor the BFF-asserted admin identity
    /// (must match each module's <c>BffAssertion:SharedSecret</c>). Empty = no assertion headers are sent — the exact
    /// mirror of the provider's <c>ModuleAssertionSecret</c>, and the safe default (feature off until configured).
    /// </summary>
    public string? ModuleAssertionSecret { get; set; }

    /// <summary>
    /// Public admin SPA client that ISSUES the admin user's Keycloak access+refresh tokens (the token <c>azp</c>).
    /// The <c>refresh_token</c> grant MUST use this exact client id. Mirrors admin-web's
    /// <c>VITE_KEYCLOAK_CLIENT_ID</c> (default <c>admin-panel</c>). The SPA login handoff, this refresh grant, and the
    /// realm's enabled public client now all name <c>admin-panel</c> (the stray config <c>AdminPanelClientId</c> was
    /// reconciled from the non-existent "admin-panel-web" to "admin-panel"; that key remains unused by BFF code).
    /// </summary>
    public string RefreshClientId { get; set; } = "admin-panel";
}
