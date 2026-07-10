using Aizen.Modules.Identity.Abstraction.Options;
using Aizen.Modules.Identity.Domain.Interface.Service;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Aizen.Modules.Identity.Repository.Identity.Service.PasswordRecovery;

/// <summary>
/// Resets a Keycloak user's password and revokes sessions via the Admin API using
/// client-credentials authentication. Mirrors the token-acquisition pattern from
/// <see cref="ProviderKeycloakRoleSyncService"/>. Never logs passwords or tokens.
/// </summary>
public sealed class IdentityKeycloakPasswordService : IIdentityKeycloakPasswordService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IdentityKeycloakOptions _options;
    private readonly ILogger<IdentityKeycloakPasswordService> _logger;

    public IdentityKeycloakPasswordService(
        IHttpClientFactory httpClientFactory,
        IOptions<IdentityKeycloakOptions> options,
        ILogger<IdentityKeycloakPasswordService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task ResetPasswordAsync(string keycloakUserId, string newPassword, CancellationToken ct)
    {
        if (!_options.Enabled)
            throw new InvalidOperationException("Keycloak integration is disabled. Password reset cannot proceed.");

        var client = await CreateClientAsync(ct);

        var credential = new { type = "password", value = newPassword, temporary = false };
        var response = await client.PutAsJsonAsync(
            $"{_options.AdminApiBaseUrl}/users/{keycloakUserId}/reset-password", credential, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task RevokeSessionsAsync(string keycloakUserId, CancellationToken ct)
    {
        if (!_options.Enabled) return;

        var client = await CreateClientAsync(ct);
        var response = await client.PostAsync(
            $"{_options.AdminApiBaseUrl}/users/{keycloakUserId}/logout", content: null, ct);
        response.EnsureSuccessStatusCode();
    }

    private async Task<HttpClient> CreateClientAsync(CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("IdentityKeycloakPasswordReset");

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = _options.AdminClientId,
            ["client_secret"] = _options.AdminClientSecret
        });

        var tokenResponse = await client.PostAsync(_options.TokenEndpoint, content, ct);
        tokenResponse.EnsureSuccessStatusCode();
        var token = await tokenResponse.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: ct)
                    ?? throw new InvalidOperationException("Keycloak returned an empty token response.");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        return client;
    }

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = default!;
    }
}
