using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Modules.Identity.Abstraction.Dto.Organizer;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.MarineProvider.Application.Common.Services;

public sealed record ProviderProfileResolution(long? ProfileId, OrganizerProfileDetailDto? Profile, string Source);

/// <summary>
/// Resolves the authenticated provider's Organizer profile id.
/// Order: provider_profile_id claim → Keycloak sub → Identity by-subject lookup → unresolved.
/// providerProfileId is never taken from the request body/query.
/// </summary>
public interface IProviderProfileResolver
{
    Task<ProviderProfileResolution> ResolveAsync(CancellationToken cancellationToken = default);
}

internal sealed class ProviderProfileResolver : IProviderProfileResolver
{
    private readonly IProviderContext _context;
    private readonly IProviderIdentityRemoteCall _identity;
    private readonly ILogger<ProviderProfileResolver> _logger;

    public ProviderProfileResolver(
        IProviderContext context,
        IProviderIdentityRemoteCall identity,
        ILogger<ProviderProfileResolver> logger)
    {
        _context = context;
        _identity = identity;
        _logger = logger;
    }

    public async Task<ProviderProfileResolution> ResolveAsync(CancellationToken cancellationToken = default)
    {
        if (_context.ProviderProfileId is { } claimId && claimId > 0)
        {
            try
            {
                var byId = await _identity.GetOrganizerProfileById(claimId);
                if (byId?.Body is not null)
                    return new ProviderProfileResolution(claimId, byId.Body, "claim");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Resolve provider profile by claim id failed.");
            }
        }

        if (!string.IsNullOrWhiteSpace(_context.KeycloakSubject))
        {
            try
            {
                var bySub = await _identity.GetOrganizerProfileByKeycloakSubject(_context.KeycloakSubject!);
                if (bySub?.Body is not null)
                    return new ProviderProfileResolution(bySub.Body.Id, bySub.Body, "subject");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Resolve provider profile by keycloak subject failed.");
            }
        }

        return new ProviderProfileResolution(null, null, "unresolved");
    }
}
