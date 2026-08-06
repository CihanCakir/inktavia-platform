using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Profile;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Profile;

/// <summary>GET /api/v1/mobile/profile/me — the authenticated participant's profile (resolved by Keycloak subject).</summary>
public sealed class GetParticipantProfileQuery : AizenQuery<GetParticipantProfileResponse>
{
}
