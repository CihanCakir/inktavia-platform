using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto.Organizer;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Query.Organizer;

public sealed class GetOrganizerProfileByKeycloakSubjectQuery : AizenQuery<OrganizerProfileDetailDto>
{
    public string KeycloakSubject { get; }

    public GetOrganizerProfileByKeycloakSubjectQuery(string keycloakSubject)
    {
        KeycloakSubject = keycloakSubject;
    }
}
