using Aizen.Core.Infrastructure.Api;
using Aizen.Core.RemoteCall.Abstraction;
using Aizen.Modules.Identity.Abstraction.Dto.Organizer;
using Aizen.Modules.Identity.Abstraction.Dto.ProviderEligibility;

namespace Aizen.Bff.Marine.Web.Application.Common.RemoteClients;

/// <summary>
/// BFF → Identity module calls. The Keycloak service token is injected by
/// <c>MarineWebBffAuthDelegatingHandler</c>; the marine-web-bff service account must hold the
/// identity_read client role on identity-api.
///
/// W2 needs a single lookup: resolve the participant profile linked to a Keycloak subject, so the BFF can assert
/// the app UserId to Content's /me endpoints. This call runs BEFORE the identity holder is populated, so it carries
/// only the service token (no assertion header) — no recursion.
/// </summary>
public interface IIdentityRemoteCall : IAizenRemoteCall
{
    // Resolve the participant profile linked to a Keycloak subject (404/null when unlinked).
    [AizenRemoteCallGet("/api/v1/identity/participant/profiles/by-subject/{keycloakSubject}")]
    Task<AizenApiResponse<OrganizerProfileDetailDto>> GetParticipantProfileByKeycloakSubject(string keycloakSubject);

    // M2 — COARSE provider availability for a (city, canonical category). Anonymous on the module (mirrors
    // providers/for-area); still sent with the service token. Returns only the bucketed verdict — never ids/count.
    [AizenRemoteCallGet("/api/v1/identity/providers/for-area/availability")]
    Task<AizenApiResponse<ProviderAreaAvailabilityDto>> GetProviderAreaAvailability(
        [Refit.Query] string cityCode, [Refit.Query] string? categoryCode = null);
}
