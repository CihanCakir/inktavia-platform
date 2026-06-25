using System.ComponentModel.DataAnnotations;

namespace Aizen.Bff.AdminPanel.Application.Common.Options;

[DocumentationInfo("Keycloak service token options", "Configuration for BFF-side Keycloak client credentials grant and service token caching.")]
public sealed class KeycloakServiceTokenOptions
{
    public const string SectionName = "KeycloakServiceToken";

    [Required(ErrorMessage = "KeycloakServiceToken:Authority is required.")]
    public string Authority { get; set; } = default!;

    [Required(ErrorMessage = "KeycloakServiceToken:TokenEndpoint is required.")]
    public string TokenEndpoint { get; set; } = default!;

    [Required(ErrorMessage = "KeycloakServiceToken:ClientId is required.")]
    public string ClientId { get; set; } = default!;

    [Required(ErrorMessage = "KeycloakServiceToken:ClientSecret is required.")]
    public string ClientSecret { get; set; } = default!;

    public int CacheSecondsBeforeExpiry { get; set; } = 60;

    [Required(ErrorMessage = "KeycloakServiceToken:CacheKeyPrefix is required.")]
    public string CacheKeyPrefix { get; set; } = "inktavia:admin-panel-bff:keycloak-service-token";
}
