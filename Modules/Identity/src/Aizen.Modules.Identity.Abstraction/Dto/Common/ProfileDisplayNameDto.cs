namespace Aizen.Modules.Identity.Abstraction.Dto.Common;

/// <summary>
/// BE-MO2c — the minimal, cost-free projection for provider/user name enrichment: a profile id and its resolved
/// display name (CompanyName first, else the person name; null when neither is set). Nothing else about the profile
/// crosses — so this can be served at <c>IdentityRead</c> (a trusted BFF service token), not Admin.
/// </summary>
public sealed class ProfileDisplayNameDto
{
    public long ProfileId { get; set; }
    public string? DisplayName { get; set; }
}
