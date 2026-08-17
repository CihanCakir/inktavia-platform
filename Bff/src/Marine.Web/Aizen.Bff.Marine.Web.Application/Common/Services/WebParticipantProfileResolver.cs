using Aizen.Bff.Marine.Web.Application.Common.RemoteClients;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Web.Application.Common.Services;

/// <summary>Result of resolving the authenticated web participant's Identity ids from the Keycloak subject.</summary>
public sealed record WebParticipantProfileResolution(long? UserId, long? ProfileId, string Source);

/// <summary>
/// Resolves the authenticated participant's Identity ids (app UserId + participant ProfileId) from the verified
/// Keycloak subject via the Identity by-subject endpoint, and populates <see cref="IWebIdentityHolder"/>.
///
/// Recursion is avoided by ordering: the by-subject lookup runs BEFORE the holder is set, so
/// <see cref="MarineWebBffAuthDelegatingHandler"/> attaches only the service token to it (no assertion header).
/// After the holder is populated, the NEXT downstream call (the Content /me engagement call) is asserted as this
/// participant — so Content derives <c>UserInfo.UserId</c> from the X-Aizen-User-Id header.
///
/// participantProfileId is NEVER taken from the request body/query — only the verified subject drives resolution.
/// Mirror of the mobile <c>ParticipantProfileResolver</c>.
/// </summary>
public interface IWebParticipantProfileResolver
{
    Task<WebParticipantProfileResolution> ResolveAsync(CancellationToken cancellationToken = default);
}

internal sealed class WebParticipantProfileResolver : IWebParticipantProfileResolver
{
    private readonly IWebParticipantContext _context;
    private readonly IIdentityRemoteCall _identity;
    private readonly IWebIdentityHolder _holder;
    private readonly ILogger<WebParticipantProfileResolver> _logger;

    public WebParticipantProfileResolver(
        IWebParticipantContext context,
        IIdentityRemoteCall identity,
        IWebIdentityHolder holder,
        ILogger<WebParticipantProfileResolver> logger)
    {
        _context = context;
        _identity = identity;
        _holder = holder;
        _logger = logger;
    }

    public async Task<WebParticipantProfileResolution> ResolveAsync(CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(_context.KeycloakSubject))
        {
            try
            {
                var bySub = await _identity.GetParticipantProfileByKeycloakSubject(_context.KeycloakSubject!);
                if (bySub?.Body is not null)
                {
                    // Sets the holder → the next downstream call carries the identity assertion headers.
                    _holder.Set(bySub.Body.UserId, bySub.Body.Id);
                    return new WebParticipantProfileResolution(bySub.Body.UserId, bySub.Body.Id, "subject");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Resolve web participant profile by keycloak subject failed.");
            }
        }

        return new WebParticipantProfileResolution(null, null, "unresolved");
    }
}
