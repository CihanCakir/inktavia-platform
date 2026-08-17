using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto.Organizer;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Query.Participant;

/// <summary>
/// Resolves a Participant profile by Keycloak subject. Reuses the shared profile-detail DTO
/// (<see cref="OrganizerProfileDetailDto"/>); null when unlinked. Used by M2d social + M3.
/// </summary>
public sealed class GetParticipantProfileByKeycloakSubjectQuery : AizenQuery<OrganizerProfileDetailDto>
{
    public string KeycloakSubject { get; }

    public GetParticipantProfileByKeycloakSubjectQuery(string keycloakSubject)
    {
        KeycloakSubject = keycloakSubject;
    }
}
