using Aizen.Bff.Marine.Web.Application.Common.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Aizen.Bff.Marine.Web.Application.Common.Services;

/// <summary>
/// Acquires and caches a Keycloak client_credentials (service account) token for the
/// marine-web-bff confidential client. Used for BFF → module calls (Authorization header)
/// and for Keycloak Admin API calls. Never logs the secret or the token.
/// </summary>
public interface IWebKeycloakServiceTokenProvider
{
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}

internal sealed class WebKeycloakServiceTokenProvider : IWebKeycloakServiceTokenProvider
{
    private const string CacheKey = "marine-web-bff:keycloak-service-token";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly MarineWebKeycloakOptions _options;

    public WebKeycloakServiceTokenProvider(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        IOptions<MarineWebKeycloakOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _options = options.Value;
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(CacheKey, out string? cached) && !string.IsNullOrEmpty(cached))
            return cached!;

        var client = _httpClientFactory.CreateClient("MarineWebKeycloak");

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = _options.AdminClientId,
            ["client_secret"] = _options.AdminClientSecret
        });

        var response = await client.PostAsync(_options.TokenEndpoint, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var token = await response.Content.ReadFromJsonAsync<KeycloakTokenResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Keycloak returned an empty token response.");

        var ttl = TimeSpan.FromSeconds(Math.Max(token.ExpiresIn - 60, 10));
        _cache.Set(CacheKey, token.AccessToken, ttl);

        return token.AccessToken;
    }

    private sealed class KeycloakTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = default!;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }
}
