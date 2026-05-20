using System.Security.Claims;

namespace Aizen.Modules.Payment.Services;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? KeycloakSubjectId =>
        _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value;

    public string? Username =>
        _httpContextAccessor.HttpContext?.User.FindFirst("preferred_username")?.Value;

    public string? Email =>
        _httpContextAccessor.HttpContext?.User.FindFirst("email")?.Value;

    public IReadOnlyCollection<string> Roles =>
        _httpContextAccessor.HttpContext?.User
            .FindAll(ClaimTypes.Role)
            .Select(x => x.Value)
            .Distinct()
            .ToArray()
        ?? Array.Empty<string>();
}
