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
}
