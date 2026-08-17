namespace Aizen.Core.InfoAccessor.Abstraction;

public interface IAizenKeycloakTokenInfoAccessor
{
    AizenKeycloakTokenInfo KeycloakTokenInfo { get; }
}

/// <summary>
/// Holds metadata extracted from a Keycloak API/service-account token.
/// Populated only when the incoming bearer token is identified as a Keycloak
/// client credential token (i.e. it does NOT carry application-user claims such as UserId).
/// </summary>
public class AizenKeycloakTokenInfo : IAizenInfo
{
    public InfoLifeCycle LifeCycle => InfoLifeCycle.Scoped;

    /// <summary>True when the current request was authenticated with a Keycloak client token.</summary>
    public bool IsPresent { get; set; }

    /// <summary>The authorized party / client-id (azp claim).</summary>
    public string ClientId { get; set; }

    /// <summary>Subject of the token (sub claim) — typically the service-account user id.</summary>
    public string Subject { get; set; }

    /// <summary>The raw bearer token string for downstream forwarding if needed.</summary>
    public string RawToken { get; set; }

    /// <summary>
    /// Provider (Organizer) profile id asserted by a trusted BFF via X-Aizen-Provider-Profile-Id.
    /// Populated only when a valid BFF assertion accompanies a Keycloak service token.
    /// </summary>
    public long? ProviderProfileId { get; set; }
}
