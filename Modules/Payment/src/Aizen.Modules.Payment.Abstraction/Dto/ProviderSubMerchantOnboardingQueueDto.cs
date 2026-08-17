using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Abstraction.Dto;

/// <summary>BE-I1 — one provider awaiting/holding a sub-merchant onboarding review (admin KYC queue row).</summary>
public sealed class ProviderSubMerchantOnboardingQueueItemDto
{
    public long    ProviderProfileId    { get; init; }
    public ProviderSubMerchantOnboardingStatus OnboardingStatus { get; init; }
    public bool    IsSplitEligible      { get; init; }
    public bool    HasIban              { get; init; }
    public string? LegalName            { get; init; }
    public string? TaxNumberMasked      { get; init; }
    public string? SubMerchantKeyMasked { get; init; }
    public DateTimeOffset? VerifiedAt   { get; init; }
    public string  Status              { get; init; } = "OnHold";
}

/// <summary>BE-I1 — paged admin sub-merchant onboarding queue.</summary>
public sealed class ProviderSubMerchantOnboardingQueueDto
{
    public IReadOnlyList<ProviderSubMerchantOnboardingQueueItemDto> Items { get; init; } = [];
    public int Total    { get; init; }
    public int Page     { get; init; }
    public int PageSize { get; init; }
}
