namespace Aizen.Modules.Identity.Abstraction.Model;

public sealed class ProvisionAdminFromKeycloakDomainModel
{
    public string KeycloakSubjectId { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool? EmailVerified { get; set; }
}
