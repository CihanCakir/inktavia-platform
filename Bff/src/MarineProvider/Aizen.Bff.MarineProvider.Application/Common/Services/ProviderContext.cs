using Aizen.Bff.MarineProvider.Application.Common.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace Aizen.Bff.MarineProvider.Application.Common.Services;

/// <summary>
/// Resolves the authenticated provider identity from the inbound Keycloak access token.
/// SECURITY: providerProfileId is NEVER taken from the request body/query — only from the
/// verified token's provider_profile_id claim (or, later, an Identity lookup by Keycloak sub).
/// </summary>
public interface IProviderContext
{
    bool IsAuthenticated { get; }
    string? KeycloakSubject { get; }
    string? Email { get; }
    string? PreferredUsername { get; }
    string? FirstName { get; }
    string? LastName { get; }
    bool EmailVerified { get; }
    long? ProviderProfileId { get; }
    bool HasProfileLink { get; }
    IReadOnlyList<string> Roles { get; }
    bool IsInRole(string role);
}

internal sealed class ProviderContext : IProviderContext
{
    private readonly ClaimsPrincipal? _user;
    private readonly MarineProviderKeycloakOptions _options;

    public ProviderContext(IHttpContextAccessor httpContextAccessor, IOptions<MarineProviderKeycloakOptions> options)
    {
        _user = httpContextAccessor.HttpContext?.User;
        _options = options.Value;
    }

    public bool IsAuthenticated => _user?.Identity?.IsAuthenticated ?? false;

    public string? KeycloakSubject => First("sub", ClaimTypes.NameIdentifier);

    public string? Email => First("email", ClaimTypes.Email);

    public string? PreferredUsername => First("preferred_username");

    public string? FirstName => First("given_name", ClaimTypes.GivenName);

    public string? LastName => First("family_name", ClaimTypes.Surname);

    public bool EmailVerified =>
        string.Equals(First("email_verified"), "true", StringComparison.OrdinalIgnoreCase);

    public long? ProviderProfileId
    {
        get
        {
            var raw = First(_options.ProviderProfileIdAttributeName);
            return long.TryParse(raw, out var id) ? id : null;
        }
    }

    public bool HasProfileLink => ProviderProfileId is > 0;

    public IReadOnlyList<string> Roles =>
        _user?.FindAll(ClaimTypes.Role).Select(c => c.Value).Distinct().ToList() ?? new List<string>();

    public bool IsInRole(string role) => _user?.IsInRole(role) ?? false;

    private string? First(params string[] claimTypes)
    {
        if (_user is null) return null;
        foreach (var type in claimTypes)
        {
            var value = _user.FindFirst(type)?.Value;
            if (!string.IsNullOrWhiteSpace(value)) return value;
        }
        return null;
    }
}
