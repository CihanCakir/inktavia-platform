namespace Aizen.Modules.Identity.Abstraction.Dto.Onboarding;

/// <summary>
/// ADDENDUM A4 — a provider who DECLARED CargoDry interest during onboarding (the optional CargoDryInterest step).
/// This is a stated wish, NOT programme participation — participation is an Active CargoDry consignment agreement,
/// owned by the CargoDry module. Admin joins this with CargoDry agreement/inventory status at the BFF layer.
/// </summary>
public sealed class CargoDryInterestApplicantDto
{
    public long      ProfileId                { get; set; }
    public long      UserId                   { get; set; }
    public string    OnboardingStatus         { get; set; } = default!;
    public DateTime? InterestDeclaredAtUtc    { get; set; }
    /// <summary>Provider's declared commercial-model preference from the draft (FE-defined free JSON), if present.</summary>
    public string?   CommercialModelPreference { get; set; }
}

public sealed class CargoDryInterestApplicantsResult
{
    public List<CargoDryInterestApplicantDto> Items { get; set; } = new();
    public int Total     { get; set; }
    public int PageIndex { get; set; }
    public int PageSize  { get; set; }
}
