namespace Aizen.Modules.Identity.Abstraction.Dto
{
    /// <summary>
    /// Generic Keycloak subject → Identity user resolution result. Returned by
    /// GET /api/v1/identity/users/by-subject/{keycloakSubject}. Null/404 when the subject is not linked to a user.
    /// Service-safe (no PII beyond the ids) — the endpoint the admin BFF service account is authorized for, used to
    /// resolve the acting admin's numeric user id for the BFF identity assertion.
    /// </summary>
    public sealed class UserBySubjectDto
    {
        public long UserId { get; set; }
        public string KeycloakSubjectId { get; set; } = default!;
    }
}
