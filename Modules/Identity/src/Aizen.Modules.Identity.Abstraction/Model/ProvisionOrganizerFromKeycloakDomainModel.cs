namespace Aizen.Modules.Identity.Abstraction.Model;

/// <summary>
/// Input for idempotently provisioning/linking an Organizer profile from a Keycloak-authenticated user.
/// No password is involved — Keycloak is the authentication authority.
/// </summary>
public sealed class ProvisionOrganizerFromKeycloakDomainModel
{
    public string KeycloakSubjectId { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? CompanyName { get; set; }
    public string? ContactPhone { get; set; }
    public string? TaxNo { get; set; }
    public bool? EmailVerified { get; set; }
}
