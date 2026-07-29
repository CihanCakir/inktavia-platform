namespace Aizen.Modules.Payment.Abstraction.Enum;

/// <summary>
/// BE-I1 — the provider sub-merchant onboarding lifecycle (§21.3). The single explicit state machine behind
/// <c>ProviderPaymentProfileEntity.IsSplitEligible</c> (the P9 split gate). The legacy string <c>Status</c>
/// ("Active"/"OnHold"/"Blocked") is kept mirrored for existing readers.
/// </summary>
public enum ProviderSubMerchantOnboardingStatus
{
    /// <summary>No onboarding data captured yet.</summary>
    NotStarted        = 0,
    /// <summary>KYC/profile data captured; sub-merchant not yet created at the gateway.</summary>
    DataSubmitted     = 1,
    /// <summary>iyzico sub-merchant created (has a key) — <b>split-eligible</b>.</summary>
    SubMerchantCreated = 2,
    /// <summary>Our own review/KYC completed — <b>split-eligible</b> (iyzico's own KYC is iyzico-side).</summary>
    Verified          = 3,
    /// <summary>Onboarding rejected — not split-eligible.</summary>
    Rejected          = 4,
    /// <summary>Temporarily suspended — not split-eligible; reactivatable.</summary>
    Suspended         = 5,
    /// <summary>Permanently blocked — not split-eligible.</summary>
    Blocked           = 6,
}
