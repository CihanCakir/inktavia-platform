namespace Aizen.Bff.AdminPanel.Application.Common.Options;

[DocumentationInfo("Keycloak service token options", "Configuration for BFF-side Keycloak client credentials grant and service token caching.")]
public sealed class KeycloakServiceTokenOptions
{
    public const string SectionName = "KeycloakServiceToken";
    public string Authority { get; set; } = default!;
    public string TokenEndpoint { get; set; } = default!;
    public string ClientId { get; set; } = default!;
    public string ClientSecret { get; set; } = default!;
    public int CacheSecondsBeforeExpiry { get; set; } = 60;
    public string CacheKeyPrefix { get; set; } = "inktavia:admin-panel-bff:keycloak-service-token";
}
