namespace Aizen.Bff.AdminPanel.Application.Common.Services;

[DocumentationInfo("Cached Keycloak service token DTO", "Stored in Redis via IAizenDistributedCache. Contains service access token and expiry tracking for the admin-panel-bff client.")]
public sealed class CachedKeycloakServiceToken
{
    public string AccessToken { get; init; } = default!;
    public string TokenType { get; init; } = "Bearer";
    public DateTimeOffset ExpiresAtUtc { get; init; }
    public DateTimeOffset RefreshAfterUtc { get; init; }
}
