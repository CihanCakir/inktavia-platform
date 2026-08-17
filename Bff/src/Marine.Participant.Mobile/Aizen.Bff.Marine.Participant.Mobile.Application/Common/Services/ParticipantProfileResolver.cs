using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Modules.Identity.Abstraction.Dto.Organizer;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;

public sealed record ParticipantProfileResolution(long? ProfileId, OrganizerProfileDetailDto? Profile, string Source);

/// <summary>
/// Resolves the authenticated participant's Identity profile from the Keycloak subject via the by-subject
/// endpoint (the non-admin route the BFF service account is authorized for). The by-subject lookup is
/// authoritative — it returns the profile id plus the UserId — and, by populating <see cref="IParticipantIdentityHolder"/>,
/// lets a subsequent asserted write (PUT participant/profile) target the correct participant. Mirror of the
/// MarineProvider <c>ProviderProfileResolver</c>. participantProfileId is never taken from the request.
/// </summary>
public interface IParticipantProfileResolver
{
    Task<ParticipantProfileResolution> ResolveAsync(CancellationToken cancellationToken = default);
}

internal sealed class ParticipantProfileResolver : IParticipantProfileResolver
{
    private readonly IParticipantContext _context;
    private readonly IIdentityRemoteCall _identity;
    private readonly IParticipantIdentityHolder _holder;
    private readonly ILogger<ParticipantProfileResolver> _logger;

    public ParticipantProfileResolver(
        IParticipantContext context,
        IIdentityRemoteCall identity,
        IParticipantIdentityHolder holder,
        ILogger<ParticipantProfileResolver> logger)
    {
        _context = context;
        _identity = identity;
        _holder = holder;
        _logger = logger;
    }

    public async Task<ParticipantProfileResolution> ResolveAsync(CancellationToken cancellationToken = default)
    {
        // Authoritative, non-admin resolution: Keycloak subject → Identity by-subject lookup. This call itself
        // runs before the holder is populated, so it carries no assertion header (no recursion). On success it
        // sets the holder, so the NEXT downstream call (the profile update) is asserted as this participant.
        if (!string.IsNullOrWhiteSpace(_context.KeycloakSubject))
        {
            try
            {
                var bySub = await _identity.GetParticipantProfileByKeycloakSubject(_context.KeycloakSubject!);
                if (bySub?.Body is not null)
                {
                    _holder.Set(bySub.Body.UserId, bySub.Body.Id);
                    return new ParticipantProfileResolution(bySub.Body.Id, bySub.Body, "subject");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Resolve participant profile by keycloak subject failed.");
            }
        }

        return new ParticipantProfileResolution(null, null, "unresolved");
    }
}
