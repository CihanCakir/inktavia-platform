using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Abstraction.Model.Result;

/// <summary>BE-I1 — result of an admin onboarding transition (verify/reject) on a provider payment profile.</summary>
public sealed record ProviderSubMerchantOnboardingResult(
    long                                ProviderProfileId,
    ProviderSubMerchantOnboardingStatus OnboardingStatus,
    bool                                IsSplitEligible);
