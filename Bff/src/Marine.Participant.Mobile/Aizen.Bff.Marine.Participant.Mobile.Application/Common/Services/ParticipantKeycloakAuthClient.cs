using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Options;
using Aizen.Core.Infrastructure.Exception;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;

/// <summary>
/// Keycloak OIDC token operations for password sign-in and session lifecycle:
///   • password verification (server-side ROPC against the confidential marine-mobile-bff client) → sub only,
///     token discarded (the real session is minted uniformly via the login_ticket + handoff);
///   • refresh / logout against the PUBLIC inktavia-mobile client (the issuer of BFF sessions).
/// Never logs passwords or tokens.
/// </summary>
public interface IParticipantKeycloakAuthClient
{
    /// <summary>Verify credentials via ROPC on marine-mobile-bff; returns the Keycloak sub or null if invalid.</summary>
    Task<string?> VerifyPasswordGetSubAsync(string email, string password, CancellationToken ct = default);
    /// <summary>Returns fresh tokens, or null if the refresh token is invalid/expired (→ caller yields 401).</summary>
    Task<HandoffTokens?> RefreshAsync(string refreshToken, CancellationToken ct = default);
    Task LogoutAsync(string refreshToken, CancellationToken ct = default);
}

internal sealed class ParticipantKeycloakAuthClient : IParticipantKeycloakAuthClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MarineMobileKeycloakOptions _options;
    private readonly ILogger<ParticipantKeycloakAuthClient> _logger;

    public ParticipantKeycloakAuthClient(
        IHttpClientFactory httpClientFactory,
        IOptions<MarineMobileKeycloakOptions> options,
        ILogger<ParticipantKeycloakAuthClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string?> VerifyPasswordGetSubAsync(string email, string password, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient("MarineMobileKeycloak");

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = _options.AdminClientId,       // confidential marine-mobile-bff
            ["client_secret"] = _options.AdminClientSecret,
            ["username"] = email,
            ["password"] = password,
            ["scope"] = "openid",
        });

        using var resp = await client.PostAsync(_options.TokenEndpoint, content, ct);
        if (resp.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.BadRequest)
            return null; // invalid credentials
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning("Password verification failed with {Status}.", (int)resp.StatusCode);
            return null;
        }

        var token = await resp.Content.ReadFromJsonAsync<KeycloakTokenResponse>(cancellationToken: ct);
        var accessToken = token?.AccessToken;
        // Token is DISCARDED — we only need the sub to mint a uniform inktavia-mobile session.
        return string.IsNullOrEmpty(accessToken) ? null : ReadSub(accessToken);
    }

    public async Task<HandoffTokens?> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient("MarineMobileKeycloak");

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = _options.ClientId,            // public inktavia-mobile (issuer of BFF sessions)
            ["refresh_token"] = refreshToken,
        });

        using var resp = await client.PostAsync(_options.TokenEndpoint, content, ct);
        if (!resp.IsSuccessStatusCode)
            return null; // invalid/expired refresh token → caller yields 401

        var token = await resp.Content.ReadFromJsonAsync<KeycloakTokenResponse>(cancellationToken: ct);
        if (token is null || string.IsNullOrEmpty(token.AccessToken))
            return null;

        return new HandoffTokens
        {
            AccessToken = token.AccessToken,
            RefreshToken = token.RefreshToken ?? refreshToken,
            ExpiresIn = token.ExpiresIn,
            TokenType = string.IsNullOrEmpty(token.TokenType) ? "Bearer" : token.TokenType,
        };
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient("MarineMobileKeycloak");

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = _options.ClientId,            // public inktavia-mobile
            ["refresh_token"] = refreshToken,
        });

        // Best-effort: Keycloak revokes the refresh token (and its session) here. A bad/expired token still
        // 204/400s without throwing to the client — logout is idempotent from the app's perspective.
        using var resp = await client.PostAsync(_options.LogoutEndpoint, content, ct);
        if (!resp.IsSuccessStatusCode)
            _logger.LogInformation("Logout returned {Status} (token may already be invalid).", (int)resp.StatusCode);
    }

    private static string? ReadSub(string jwt)
    {
        try
        {
            var parts = jwt.Split('.');
            if (parts.Length < 2) return null;
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            using var doc = JsonDocument.Parse(Convert.FromBase64String(payload));
            return doc.RootElement.TryGetProperty("sub", out var sub) ? sub.GetString() : null;
        }
        catch
        {
            return null;
        }
    }

    private sealed class KeycloakTokenResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("access_token")] public string AccessToken { get; set; } = default!;
        [System.Text.Json.Serialization.JsonPropertyName("refresh_token")] public string? RefreshToken { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("token_type")] public string? TokenType { get; set; }
    }
}
