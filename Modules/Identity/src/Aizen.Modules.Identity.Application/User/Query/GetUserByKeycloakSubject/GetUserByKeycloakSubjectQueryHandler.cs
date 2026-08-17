using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto;
using Aizen.Modules.Identity.Domain.Interface.Repository;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Query.User;

/// <summary>
/// Resolves an Identity user by Keycloak subject and maps it to the service-safe id DTO. A generic mirror of
/// <c>GetOrganizerProfileByKeycloakSubjectQueryHandler</c> — data access via <see cref="IUserRepository"/>, no
/// CQRS-in-CQRS dispatch. Null when the subject is not linked to a user.
/// </summary>
public sealed class GetUserByKeycloakSubjectQueryHandler
    : AizenQueryHandler<GetUserByKeycloakSubjectQuery, UserBySubjectDto>
{
    private readonly IUserRepository _users;

    public GetUserByKeycloakSubjectQueryHandler(IUserRepository users)
    {
        _users = users;
    }

    public override async Task<UserBySubjectDto?> Handle(
        GetUserByKeycloakSubjectQuery request, CancellationToken cancellationToken)
    {
        var user = await _users.GetUserByKeycloakSubjectAsync(request.KeycloakSubject, cancellationToken);
        if (user is null || string.IsNullOrWhiteSpace(user.KeycloakSubjectId))
            return null;

        return new UserBySubjectDto
        {
            UserId = user.Id,
            KeycloakSubjectId = user.KeycloakSubjectId
        };
    }
}
