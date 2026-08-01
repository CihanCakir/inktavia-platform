using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.AdminPanel.Application.Common.Services;

/// <summary>
/// Resolves the authenticated admin's numeric Identity user id from the Keycloak subject via the by-subject Identity
/// endpoint (the non-admin endpoint the BFF service account is authorized for), then populates
/// <see cref="IAdminIdentityHolder"/>. A mirror of the MarineProvider BFF's <c>ProviderProfileResolver</c> minus the
/// profile id: the by-subject lookup is authoritative and returns the numeric UserId required for the identity
/// assertion, so no Keycloak user-id claim mapper is needed. The subject comes ONLY from the verified token.
///
/// Because the holder is populated only AFTER this Identity call completes, the resolver's own call carries no
/// assertion headers → no recursion. Idempotent — resolves at most once per request.
/// </summary>
public interface IAdminIdentityResolver
{
    Task ResolveAsync(CancellationToken cancellationToken = default);
}

internal sealed class AdminIdentityResolver : IAdminIdentityResolver
{
    private readonly IAdminContext _context;
    private readonly IIdentityAdminBffRemoteCall _identity;
    private readonly IAdminIdentityHolder _holder;
    private readonly ILogger<AdminIdentityResolver> _logger;

    // Re-entrancy guard (scoped instance = one per request): the by-subject call below flows back through the same
    // delegating handler, which calls ResolveAsync again. This makes that inner call a no-op, so the by-subject
    // request carries only the service token (no assertion) and there is no recursion — matching the provider, whose
    // resolver call likewise runs before the holder is populated.
    private bool _resolving;

    public AdminIdentityResolver(
        IAdminContext context,
        IIdentityAdminBffRemoteCall identity,
        IAdminIdentityHolder holder,
        ILogger<AdminIdentityResolver> logger)
    {
        _context = context;
        _identity = identity;
        _holder = holder;
        _logger = logger;
    }

    public async Task ResolveAsync(CancellationToken cancellationToken = default)
    {
        if (_holder.Resolved || _resolving)
            return;

        _resolving = true;
        try
        {
            // Authoritative, non-admin resolution: Keycloak subject → Identity by-subject lookup. Returns the numeric
            // UserId needed for the identity assertion. Every linked admin token carries a subject.
            if (!string.IsNullOrWhiteSpace(_context.KeycloakSubject))
            {
                try
                {
                    var bySub = await _identity.GetUserByKeycloakSubject(_context.KeycloakSubject!);
                    if (bySub?.Body is { UserId: > 0 })
                    {
                        _holder.Set(bySub.Body.UserId);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Resolve admin user by keycloak subject failed.");
                }
            }

            _holder.Set(null);
        }
        finally
        {
            _resolving = false;
        }
    }
}
