namespace Aizen.Bff.MarineProvider.Application.Contracts.Me;

public sealed class GetProviderMeResponse
{
    public string? KeycloakSubject { get; set; }
    public string? Email { get; set; }
    public string? PreferredUsername { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool EmailVerified { get; set; }
    public long? ProviderProfileId { get; set; }
    public bool HasProfileLink { get; set; }
    public IReadOnlyList<string> Roles { get; set; } = new List<string>();
}
