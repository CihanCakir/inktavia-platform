using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Query.GetCargoDryProgramApplications;

/// <summary>
/// ADDENDUM A4 — admin "CargoDry programme applications" queue: providers who DECLARED CargoDry interest during
/// onboarding (Identity), enriched with their CargoDry agreement/inventory status (CargoDry). Interest is a declared
/// WISH, not participation — participation is an Active consignment agreement (ActiveAgreementCount > 0 / IsParticipant).
/// </summary>
public sealed class GetCargoDryProgramApplicationsBffQuery : AizenQuery<GetCargoDryProgramApplicationsBffResponse>
{
    public int PageIndex { get; init; } = 0;
    public int PageSize  { get; init; } = 25;
}

public sealed class GetCargoDryProgramApplicationsBffResponse
{
    public List<CargoDryProgramApplicationBffDto> Items { get; init; } = new();
    public int Total     { get; init; }
    public int PageIndex { get; init; }
    public int PageSize  { get; init; }
}

public sealed class CargoDryProgramApplicationBffDto
{
    public long      ProfileId                 { get; init; }
    public long      UserId                    { get; init; }
    public string    OnboardingStatus          { get; init; } = default!;
    /// <summary>Declared interest — a WISH, not participation.</summary>
    public DateTime? InterestDeclaredAtUtc     { get; init; }
    public string?   CommercialModelPreference { get; init; }
    // CargoDry-side enrichment (participation = the authoritative record):
    public int       ActiveAgreementCount      { get; init; }
    public bool      IsParticipant             { get; init; }
    public bool      HasInventory              { get; init; }
}
