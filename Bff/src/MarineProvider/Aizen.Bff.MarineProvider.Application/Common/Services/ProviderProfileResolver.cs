using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Modules.Identity.Abstraction.Dto.Organizer;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.MarineProvider.Application.Common.Services;

public sealed record ProviderProfileResolution(long? ProfileId, OrganizerProfileDetailDto? Profile, string Source);

/// <summary>
/// Resolves the authenticated provider's Organizer profile id from the Keycloak subject via the by-subject
/// Identity endpoint (the non-admin endpoint the BFF service account is authorized for). The by-subject lookup
/// is authoritative — it returns the profile id plus the UserId required for the identity assertion — so the
/// provider_profile_id claim is not resolved through the admin-only by-id endpoint (that call always 403s for the
/// BFF service account). providerProfileId is never taken from the request body/query.
/// </summary>
public interface IProviderProfileResolver
{
    Task<ProviderProfileResolution> ResolveAsync(CancellationToken cancellationToken = default);
}

internal sealed class ProviderProfileResolver : IProviderProfileResolver
{
    private readonly IProviderContext _context;
    private readonly IIdentityRemoteCall _identity;
    private readonly IProviderIdentityHolder _holder;
    private readonly ILogger<ProviderProfileResolver> _logger;

    public ProviderProfileResolver(
        IProviderContext context,
        IIdentityRemoteCall identity,
        IProviderIdentityHolder holder,
        ILogger<ProviderProfileResolver> logger)
    {
        _context = context;
        _identity = identity;
        _holder = holder;
        _logger = logger;
    }

    public async Task<ProviderProfileResolution> ResolveAsync(CancellationToken cancellationToken = default)
    {
        // Authoritative, non-admin resolution: Keycloak subject → Identity by-subject lookup.
        // Every linked provider token carries a subject, and linking is keyed by subject, so this path resolves
        // whenever a profile exists. It returns the profile id and the UserId needed for the identity assertion.
        if (!string.IsNullOrWhiteSpace(_context.KeycloakSubject))
        {
            try
            {
                var bySub = await _identity.GetOrganizerProfileByKeycloakSubject(_context.KeycloakSubject!);
                if (bySub?.Body is not null)
                {
                    _holder.Set(bySub.Body.UserId, bySub.Body.Id);
                    return new ProviderProfileResolution(bySub.Body.Id, bySub.Body, "subject");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Resolve provider profile by keycloak subject failed.");
            }
        }

        return new ProviderProfileResolution(null, null, "unresolved");
    }
}
