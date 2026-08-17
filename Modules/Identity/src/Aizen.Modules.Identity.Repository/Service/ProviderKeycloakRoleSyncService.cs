using Aizen.Modules.Identity.Abstraction.Options;
using Aizen.Modules.Identity.Domain.Interface.Service;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Aizen.Modules.Identity.Repository.Identity.Service
{
    /// <summary>
    /// Direct Keycloak Admin API role sync (client_credentials). Called explicitly after successful
    /// Identity approval/suspend. Failures are logged and swallowed so Identity status is never corrupted;
    /// move to an outbox/retry consumer in a later phase. Never logs secrets or tokens.
    /// </summary>
    public sealed class ProviderKeycloakRoleSyncService : IProviderKeycloakRoleSyncService
    {
        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IdentityKeycloakOptions _options;
        private readonly ILogger<ProviderKeycloakRoleSyncService> _logger;

        public ProviderKeycloakRoleSyncService(
            IHttpClientFactory httpClientFactory,
            IOptions<IdentityKeycloakOptions> options,
            ILogger<ProviderKeycloakRoleSyncService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _options = options.Value;
            _logger = logger;
        }

        public async Task OnApprovedAsync(string? keycloakSubjectId, CancellationToken cancellationToken = default)
        {
            if (!ShouldRun(keycloakSubjectId)) return;
            var userId = keycloakSubjectId!;

            await RemoveRoleAsync(userId, _options.ProviderPendingRole, cancellationToken);
            await AddRoleAsync(userId, _options.ProviderUserRole, cancellationToken);
            await RemoveRoleAsync(userId, _options.ProviderRestrictedRole, cancellationToken);
        }

        public async Task OnSuspendedAsync(string? keycloakSubjectId, CancellationToken cancellationToken = default)
        {
            if (!ShouldRun(keycloakSubjectId)) return;
            var userId = keycloakSubjectId!;

            await RemoveRoleAsync(userId, _options.ProviderUserRole, cancellationToken);
            await AddRoleAsync(userId, _options.ProviderRestrictedRole, cancellationToken);

            if (_options.RevokeSessionsOnSuspend)
                await LogoutUserAsync(userId, cancellationToken);
        }

        public async Task OnReactivatedAsync(string? keycloakSubjectId, CancellationToken cancellationToken = default)
        {
            if (!ShouldRun(keycloakSubjectId)) return;
            var userId = keycloakSubjectId!;

            await AddRoleAsync(userId, _options.ProviderUserRole, cancellationToken);
            await RemoveRoleAsync(userId, _options.ProviderRestrictedRole, cancellationToken);
        }

        private bool ShouldRun(string? keycloakSubjectId)
        {
            if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.BaseUrl))
                return false;
            if (string.IsNullOrWhiteSpace(keycloakSubjectId))
                return false;
            return true;
        }

        private async Task<HttpClient> CreateClientAsync(CancellationToken ct)
        {
            var client = _httpClientFactory.CreateClient("IdentityKeycloakRoleSync");

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

        private async Task AddRoleAsync(string userId, string roleName, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(roleName)) return;
            try
            {
                var client = await CreateClientAsync(ct);
                var role = await client.GetFromJsonAsync<RoleRepresentation>(
                    $"{_options.AdminApiBaseUrl}/roles/{Uri.EscapeDataString(roleName)}", Json, ct);
                if (role is null) return;

                var response = await client.PostAsJsonAsync(
                    $"{_options.AdminApiBaseUrl}/users/{userId}/role-mappings/realm", new[] { role }, Json, ct);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Keycloak role add failed (role {Role}, user {UserId}).", roleName, userId);
            }
        }

        private async Task RemoveRoleAsync(string userId, string roleName, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(roleName)) return;
            try
            {
                var client = await CreateClientAsync(ct);
                var role = await client.GetFromJsonAsync<RoleRepresentation>(
                    $"{_options.AdminApiBaseUrl}/roles/{Uri.EscapeDataString(roleName)}", Json, ct);
                if (role is null) return;

                using var request = new HttpRequestMessage(
                    HttpMethod.Delete, $"{_options.AdminApiBaseUrl}/users/{userId}/role-mappings/realm")
                {
                    Content = JsonContent.Create(new[] { role }, options: Json)
                };
                var response = await client.SendAsync(request, ct);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Keycloak role remove failed (role {Role}, user {UserId}).", roleName, userId);
            }
        }

        private async Task LogoutUserAsync(string userId, CancellationToken ct)
        {
            try
            {
                var client = await CreateClientAsync(ct);
                var response = await client.PostAsync($"{_options.AdminApiBaseUrl}/users/{userId}/logout", null, ct);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Keycloak session revoke failed for user {UserId}.", userId);
            }
        }

        private sealed class TokenResponse
        {
            [JsonPropertyName("access_token")]
            public string AccessToken { get; set; } = default!;
        }

        private sealed class RoleRepresentation
        {
            public string? Id { get; set; }
            public string? Name { get; set; }
        }
    }
}
