using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Query.User;

public sealed class GetUserByKeycloakSubjectQuery : AizenQuery<UserBySubjectDto>
{
    public string KeycloakSubject { get; }

    public GetUserByKeycloakSubjectQuery(string keycloakSubject)
    {
        KeycloakSubject = keycloakSubject;
    }
}
