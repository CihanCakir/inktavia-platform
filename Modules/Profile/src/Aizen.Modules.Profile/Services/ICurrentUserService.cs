namespace Aizen.Modules.Profile.Services;

public interface ICurrentUserService
{
    /// <summary>
    /// Keycloak user subject id (sub claim). Used to map to local UserEntity.KeycloakSubjectId.
    /// </summary>
    string? KeycloakSubjectId { get; }

    string? Username { get; }

    string? Email { get; }

    IReadOnlyCollection<string> Roles { get; }
}
