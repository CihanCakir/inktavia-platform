using Aizen.Bff.MarineProvider.Application.Common.Options;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Aizen.Bff.MarineProvider.Application.Common.Services;

public sealed record KeycloakUserRef(string Id, string? Email, string? Username, bool EmailVerified);

public sealed record CreateKeycloakUserRequest(
    string Email,
    string Password,
    string? FirstName,
    string? LastName,
    IDictionary<string, string>? Attributes);

/// <summary>
/// Thin Keycloak Admin REST client used by the MarineProvider BFF for provider provisioning.
/// Authenticated with the provider-portal-bff service account token (realm-management roles required).
/// Never logs secrets, tokens, passwords, or verification links.
/// </summary>
public interface IProviderKeycloakAdminClient
{
    Task<KeycloakUserRef?> FindUserByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<KeycloakUserRef?> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<string> CreateUserAsync(CreateKeycloakUserRequest request, CancellationToken cancellationToken = default);
    Task SetUserAttributeAsync(string userId, string attributeName, string attributeValue, CancellationToken cancellationToken = default);
    Task AssignRealmRoleAsync(string userId, string roleName, CancellationToken cancellationToken = default);
    Task SendVerifyEmailAsync(string userId, CancellationToken cancellationToken = default);
}

internal sealed class ProviderKeycloakAdminClient : IProviderKeycloakAdminClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IProviderKeycloakServiceTokenProvider _tokenProvider;
    private readonly MarineProviderKeycloakOptions _options;

    public ProviderKeycloakAdminClient(
        IHttpClientFactory httpClientFactory,
        IProviderKeycloakServiceTokenProvider tokenProvider,
        IOptions<MarineProviderKeycloakOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _tokenProvider = tokenProvider;
        _options = options.Value;
    }

    private async Task<HttpClient> CreateClientAsync(CancellationToken ct)
    {
        var token = await _tokenProvider.GetAccessTokenAsync(ct);
        var client = _httpClientFactory.CreateClient("ProviderKeycloak");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public async Task<KeycloakUserRef?> FindUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var client = await CreateClientAsync(cancellationToken);
        var url = $"{_options.AdminApiBaseUrl}/users?email={Uri.EscapeDataString(email)}&exact=true";
        var users = await client.GetFromJsonAsync<List<UserRepresentation>>(url, Json, cancellationToken);
        var user = users?.FirstOrDefault();
        return user is null ? null : Map(user);
    }

    public async Task<KeycloakUserRef?> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var client = await CreateClientAsync(cancellationToken);
        var response = await client.GetAsync($"{_options.AdminApiBaseUrl}/users/{userId}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        var user = await response.Content.ReadFromJsonAsync<UserRepresentation>(Json, cancellationToken);
        return user is null ? null : Map(user);
    }

    public async Task<string> CreateUserAsync(CreateKeycloakUserRequest request, CancellationToken cancellationToken = default)
    {
        var client = await CreateClientAsync(cancellationToken);

        var representation = new UserRepresentation
        {
            Username = request.Email,
            Email = request.Email,
            Enabled = true,
            EmailVerified = false,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Credentials = new List<CredentialRepresentation>
            {
                new() { Type = "password", Value = request.Password, Temporary = false }
            },
            Attributes = request.Attributes?.ToDictionary(kv => kv.Key, kv => new List<string> { kv.Value })
        };

        var response = await client.PostAsJsonAsync($"{_options.AdminApiBaseUrl}/users", representation, Json, cancellationToken);
        response.EnsureSuccessStatusCode();

        var location = response.Headers.Location?.ToString();
        if (!string.IsNullOrWhiteSpace(location))
            return location.TrimEnd('/').Split('/').Last();

        // Fallback when Keycloak omits the Location header.
        var created = await FindUserByEmailAsync(request.Email, cancellationToken);
        return created?.Id
            ?? throw new InvalidOperationException("Keycloak user created but its id could not be resolved.");
    }

    public async Task SetUserAttributeAsync(string userId, string attributeName, string attributeValue, CancellationToken cancellationToken = default)
    {
        var client = await CreateClientAsync(cancellationToken);

        var current = await client.GetFromJsonAsync<UserRepresentation>(
                          $"{_options.AdminApiBaseUrl}/users/{userId}", Json, cancellationToken)
                      ?? throw new InvalidOperationException("Keycloak user not found for attribute update.");

        current.Attributes ??= new Dictionary<string, List<string>>();
        current.Attributes[attributeName] = new List<string> { attributeValue };

        var response = await client.PutAsJsonAsync($"{_options.AdminApiBaseUrl}/users/{userId}", current, Json, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task AssignRealmRoleAsync(string userId, string roleName, CancellationToken cancellationToken = default)
    {
        var client = await CreateClientAsync(cancellationToken);

        var role = await client.GetFromJsonAsync<RoleRepresentation>(
                       $"{_options.AdminApiBaseUrl}/roles/{Uri.EscapeDataString(roleName)}", Json, cancellationToken)
                   ?? throw new InvalidOperationException($"Keycloak realm role '{roleName}' not found.");

        var response = await client.PostAsJsonAsync(
            $"{_options.AdminApiBaseUrl}/users/{userId}/role-mappings/realm",
            new[] { role }, Json, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task SendVerifyEmailAsync(string userId, CancellationToken cancellationToken = default)
    {
        var client = await CreateClientAsync(cancellationToken);

        var url = $"{_options.AdminApiBaseUrl}/users/{userId}/execute-actions-email";
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(_options.ProviderPortalClientId))
            query.Add($"client_id={Uri.EscapeDataString(_options.ProviderPortalClientId)}");
        if (!string.IsNullOrWhiteSpace(_options.VerifyEmailRedirectUri))
            query.Add($"redirect_uri={Uri.EscapeDataString(_options.VerifyEmailRedirectUri!)}");
        if (query.Count > 0)
            url += "?" + string.Join("&", query);

        var response = await client.PutAsJsonAsync(url, new[] { "VERIFY_EMAIL" }, Json, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private static KeycloakUserRef Map(UserRepresentation user) =>
        new(user.Id!, user.Email, user.Username, user.EmailVerified);

    // ── Keycloak Admin REST representations (minimal subset) ────────────────
    private sealed class UserRepresentation
    {
        public string? Id { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public bool Enabled { get; set; }
        public bool EmailVerified { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public List<CredentialRepresentation>? Credentials { get; set; }
        public Dictionary<string, List<string>>? Attributes { get; set; }
    }

    private sealed class CredentialRepresentation
    {
        public string? Type { get; set; }
        public string? Value { get; set; }
        public bool Temporary { get; set; }
    }

    private sealed class RoleRepresentation
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
    }
}
