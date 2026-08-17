using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Aizen.Bff.AdminPanel.Application.Common.Services;

/// <summary>
/// Resolves the authenticated admin identity from the inbound Keycloak access token. A UserId-only mirror of the
/// MarineProvider BFF's <c>IProviderContext</c> — an admin is not a provider, so it exposes only the Keycloak subject
/// (used to resolve the numeric Identity user id by-subject for the BFF assertion). Read only from the verified token.
/// </summary>
public interface IAdminContext
{
    bool IsAuthenticated { get; }
    string? KeycloakSubject { get; }
}

internal sealed class AdminContext : IAdminContext
{
    private readonly ClaimsPrincipal? _user;

    public AdminContext(IHttpContextAccessor httpContextAccessor)
    {
        _user = httpContextAccessor.HttpContext?.User;
    }

    public bool IsAuthenticated => _user?.Identity?.IsAuthenticated ?? false;

    public string? KeycloakSubject => First("sub", ClaimTypes.NameIdentifier);

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
