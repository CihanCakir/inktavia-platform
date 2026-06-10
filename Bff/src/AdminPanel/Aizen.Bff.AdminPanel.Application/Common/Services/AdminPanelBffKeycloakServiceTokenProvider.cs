using Aizen.Bff.AdminPanel.Application.Common.Options;
using Aizen.Core.Cache.Abstraction;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Aizen.Bff.AdminPanel.Application.Common.Services;

[DocumentationInfo("BFF Keycloak service token provider implementation", "Acquires and caches the admin-panel-bff Keycloak client_credentials token using IAizenDistributedCache with TTL = expires_in - CacheSecondsBeforeExpiry.")]
internal sealed class AdminPanelBffKeycloakServiceTokenProvider : IAdminPanelBffKeycloakServiceTokenProvider
{
    private readonly IAizenDistributedCache _cache;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly KeycloakServiceTokenOptions _options;
    private readonly ILogger<AdminPanelBffKeycloakServiceTokenProvider> _logger;

    public AdminPanelBffKeycloakServiceTokenProvider(
        IAizenDistributedCache cache,
        IHttpClientFactory httpClientFactory,
        IOptions<KeycloakServiceTokenOptions> options,
        ILogger<AdminPanelBffKeycloakServiceTokenProvider> logger)
    {
        _cache = cache;
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{_options.CacheKeyPrefix}:{_options.ClientId}:default";

        if (await _cache.ExistNoHash(cacheKey))
        {
            var cached = await _cache.GetNoHash<CachedKeycloakServiceToken>(cacheKey);
            if (cached is not null && cached.RefreshAfterUtc > DateTimeOffset.UtcNow)
                return cached.AccessToken;
        }

        var tokenResponse = await FetchTokenFromKeycloakAsync(cancellationToken);

        var ttlSeconds = Math.Max(tokenResponse.ExpiresIn - _options.CacheSecondsBeforeExpiry, 10);
        var ttl = TimeSpan.FromSeconds(ttlSeconds);
        var now = DateTimeOffset.UtcNow;

        var entry = new CachedKeycloakServiceToken
        {
            AccessToken = tokenResponse.AccessToken,
            TokenType = tokenResponse.TokenType ?? "Bearer",
            ExpiresAtUtc = now.AddSeconds(tokenResponse.ExpiresIn),
            RefreshAfterUtc = now.AddSeconds(ttlSeconds)
        };

        await _cache.SetNoHash(cacheKey, entry, ttl);

        _logger.LogDebug("Keycloak service token cached for client {ClientId}, TTL {Ttl}s", _options.ClientId, ttlSeconds);

        return entry.AccessToken;
    }

    private async Task<KeycloakTokenResponse> FetchTokenFromKeycloakAsync(CancellationToken cancellationToken)
    {
        using var client = _httpClientFactory.CreateClient("KeycloakServiceToken");
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret
        });

        var response = await client.PostAsync(_options.TokenEndpoint, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var tokenResponse = await response.Content.ReadFromJsonAsync<KeycloakTokenResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Keycloak returned an empty token response.");

        return tokenResponse;
    }

    private sealed class KeycloakTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = default!;
        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }
        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }
}
