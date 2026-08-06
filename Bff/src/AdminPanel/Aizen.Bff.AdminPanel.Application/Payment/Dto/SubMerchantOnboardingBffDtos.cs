using Aizen.Modules.Payment.Abstraction.Dto;
using Aizen.Modules.Payment.Abstraction.Model.Result;

namespace Aizen.Bff.AdminPanel.Application.Payment.Dto;

/// <summary>BE-I1 — admin reject-sub-merchant body forwarded to the Payment module (typed, never object).</summary>
public sealed record RejectSubMerchantBffRequest(string? Reason);

/// <summary>BE-I1 — BFF wrapper for the paged sub-merchant onboarding queue (envelope carries this typed body).</summary>
public sealed class GetSubMerchantOnboardingQueueBffResponse
{
    public ProviderSubMerchantOnboardingQueueDto Result { get; init; } = default!;
}

/// <summary>BE-I1 — BFF wrapper for a verify/reject onboarding transition result.</summary>
public sealed class SubMerchantOnboardingMutateBffResponse
{
    public ProviderSubMerchantOnboardingResult Result { get; init; } = default!;
}
