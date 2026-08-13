using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Aizen.Bff.AdminPanel.Application.Common.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aizen.Bff.AdminPanel.Application.Common.Services;

/// <summary>
/// Keycloak OIDC refresh for the admin panel. The admin browser session is a Keycloak-issued access+refresh token
/// pair, minted via the OTP → <c>login_ticket</c> handoff against the PUBLIC <c>admin-panel</c> client. Session
/// refresh therefore MUST go to Keycloak's <c>refresh_token</c> grant on that same public client — NOT the Identity
/// <c>UserLoginTokenEntity</c> store, which never held these tokens (→ <c>TokenNotFound</c>). This mirrors the mobile
/// BFF's <c>ParticipantKeycloakAuthClient.RefreshAsync</c>. Never logs tokens.
/// </summary>
public interface IAdminKeycloakAuthClient
{
    /// <summary>Fresh tokens, or null if the refresh token is invalid/expired (→ caller yields a fail envelope / 401).</summary>
    Task<AdminKeycloakTokens?> RefreshAsync(string refreshToken, CancellationToken ct = default);
}

public sealed class AdminKeycloakTokens
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public string TokenType { get; set; } = "Bearer";
}

internal sealed class AdminKeycloakAuthClient : IAdminKeycloakAuthClient
{
    /// <summary>Named HttpClient (kept for parity with the mobile BFF; AddHttpClient() supplies a default client).</summary>
    public const string HttpClientName = "AdminPanelKeycloak";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly KeycloakServiceTokenOptions _serviceToken;
    private readonly AdminPanelKeycloakOptions _keycloak;
    private readonly ILogger<AdminKeycloakAuthClient> _logger;

    public AdminKeycloakAuthClient(
        IHttpClientFactory httpClientFactory,
        IOptions<KeycloakServiceTokenOptions> serviceToken,
        IOptions<AdminPanelKeycloakOptions> keycloak,
        ILogger<AdminKeycloakAuthClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _serviceToken = serviceToken.Value;
        _keycloak = keycloak.Value;
        _logger = logger;
    }

    public async Task<AdminKeycloakTokens?> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return null;

        var client = _httpClientFactory.CreateClient(HttpClientName);

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = _keycloak.RefreshClientId,   // PUBLIC admin-panel client — the issuer of the admin session
            ["refresh_token"] = refreshToken,
        });

        // Same realm token endpoint the service-token provider uses (env-wired, validated on start).
        using var resp = await client.PostAsync(_serviceToken.TokenEndpoint, content, ct);
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogInformation("Admin token refresh returned {Status} (refresh token invalid/expired).", (int)resp.StatusCode);
            return null; // invalid/expired refresh token → caller yields a fail envelope
        }

        var token = await resp.Content.ReadFromJsonAsync<KeycloakTokenResponse>(cancellationToken: ct);
        if (token is null || string.IsNullOrEmpty(token.AccessToken))
            return null;

        return new AdminKeycloakTokens
        {
            AccessToken = token.AccessToken,
            // Keycloak rotates the refresh token; return the new one so the client persists the latest.
            RefreshToken = token.RefreshToken ?? refreshToken,
            ExpiresIn = token.ExpiresIn,
            TokenType = string.IsNullOrEmpty(token.TokenType) ? "Bearer" : token.TokenType,
        };
    }

    private sealed class KeycloakTokenResponse
    {
        [JsonPropertyName("access_token")] public string AccessToken { get; set; } = default!;
        [JsonPropertyName("refresh_token")] public string? RefreshToken { get; set; }
        [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
        [JsonPropertyName("token_type")] public string? TokenType { get; set; }
    }
}
