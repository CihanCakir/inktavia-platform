namespace Aizen.Core.InfoAccessor.Abstraction;

/// <summary>
/// Trusted-BFF identity assertion settings (module side). When <see cref="SharedSecret"/> is empty the
/// feature is DISABLED (default) and the middleware behaves exactly as before — safe to ship un-configured.
/// Bound from configuration section <c>BffAssertion</c>.
/// </summary>
public sealed class AizenBffAssertionOptions
{
    public const string SectionName = "BffAssertion";

    /// <summary>Shared secret the trusted BFF must present in the X-Aizen-Bff-Assertion header. Empty = disabled.</summary>
    public string SharedSecret { get; set; } = string.Empty;

    /// <summary>Optional additional gate: only honor assertions when the Keycloak token azp is in this list.</summary>
    public string[] AllowedClientIds { get; set; } = System.Array.Empty<string>();
}
