using Aizen.Core.Domain;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Domain.Entities.PaymentProfile;

[DocumentationInfo("Provider payment profile entity",
    "Stores a provider's payment/payout identity — Iyzico sub-merchant account details, IBAN (encrypted), and the " +
    "onboarding lifecycle (BE-I1) that drives split-eligibility. The legacy string Status is mirrored for existing readers.")]
public sealed class ProviderPaymentProfileEntity : AizenEntityWithAudit
{
    public long    ProviderProfileId    { get; private set; }  // 1:1 with provider
    public string  GatewayProvider      { get; private set; } = default!;  // "manual" | "iyzico"
    public string? SubMerchantKey       { get; private set; }  // Iyzico subMerchantKey (masked in APIs)
    public string? SubMerchantAccountId { get; private set; }  // Iyzico accountId
    public string? IbanEncrypted        { get; private set; }  // AES-256 encrypted IBAN
    public string? IbanLast4            { get; private set; }  // Last 4 of raw IBAN for masked display
    public string? LegalName            { get; private set; }
    public string? TaxNumber            { get; private set; }  // Masked in APIs
    public string  Status               { get; private set; } = "Active";  // legacy mirror: Active | OnHold | Blocked

    /// <summary>BE-I1 onboarding state machine — the source of truth behind <see cref="IsSplitEligible"/>.</summary>
    public ProviderSubMerchantOnboardingStatus OnboardingStatus { get; private set; }
        = ProviderSubMerchantOnboardingStatus.NotStarted;

    public DateTime? VerifiedAt         { get; private set; }

    /// <summary>
    /// The single split-eligibility gate signal (§21.4). Split-eligible ⇔ a sub-merchant key exists AND onboarding is in
    /// {SubMerchantCreated, Verified}. Being in that set inherently excludes Suspended/Blocked/Rejected. Documented
    /// decision: an iyzico-created sub-merchant can already receive a split, so <c>SubMerchantCreated</c> is eligible;
    /// <c>Verified</c> is the point our own review (if any) completes — real iyzico KYC is iyzico-side.
    /// </summary>
    public bool IsSplitEligible =>
        !string.IsNullOrWhiteSpace(SubMerchantKey)
        && !string.IsNullOrWhiteSpace(IbanEncrypted)   // BE-P9-fix §5: iyzico requires an IBAN before product approval
        && OnboardingStatus is ProviderSubMerchantOnboardingStatus.SubMerchantCreated
                            or ProviderSubMerchantOnboardingStatus.Verified;

    private ProviderPaymentProfileEntity() { }

    public static ProviderPaymentProfileEntity Create(
        long providerProfileId, string gatewayProvider,
        string? legalName = null, string? taxNumber = null)
    {
        return new ProviderPaymentProfileEntity
        {
            ProviderProfileId = providerProfileId,
            GatewayProvider   = gatewayProvider,
            LegalName         = legalName,
            TaxNumber         = taxNumber,
            OnboardingStatus  = ProviderSubMerchantOnboardingStatus.NotStarted,
            Status            = "Active",
            IsActive          = true,
        };
    }

    // ── Onboarding lifecycle (§21.3) — guarded transitions; legacy Status mirrored ──

    /// <summary>NotStarted / Rejected → DataSubmitted (KYC/profile data captured).</summary>
    public void SubmitOnboardingData()
    {
        Ensure(OnboardingStatus is ProviderSubMerchantOnboardingStatus.NotStarted
                                or ProviderSubMerchantOnboardingStatus.Rejected);
        Transition(ProviderSubMerchantOnboardingStatus.DataSubmitted);
    }

    /// <summary>
    /// {NotStarted, DataSubmitted, Rejected} → SubMerchantCreated. Folds the former <c>RegisterSubMerchant</c>: sets the
    /// gateway key/account. Idempotent — re-invocation with the same key while already Created/Verified is a no-op.
    /// Does NOT set <see cref="VerifiedAt"/> — that is <see cref="MarkVerified"/>.
    /// </summary>
    public void MarkSubMerchantCreated(string subMerchantKey, string? accountId)
    {
        // Idempotent no-op: already onboarded with the same key.
        if (SubMerchantKey == subMerchantKey
            && OnboardingStatus is ProviderSubMerchantOnboardingStatus.SubMerchantCreated
                                or ProviderSubMerchantOnboardingStatus.Verified)
            return;

        Ensure(OnboardingStatus is ProviderSubMerchantOnboardingStatus.NotStarted
                                or ProviderSubMerchantOnboardingStatus.DataSubmitted
                                or ProviderSubMerchantOnboardingStatus.Rejected);
        SubMerchantKey       = subMerchantKey;
        SubMerchantAccountId = accountId;
        Transition(ProviderSubMerchantOnboardingStatus.SubMerchantCreated);
    }

    /// <summary>SubMerchantCreated → Verified (our review complete). Stamps <see cref="VerifiedAt"/>.</summary>
    public void MarkVerified()
    {
        Ensure(OnboardingStatus is ProviderSubMerchantOnboardingStatus.SubMerchantCreated);
        VerifiedAt = DateTime.UtcNow;
        Transition(ProviderSubMerchantOnboardingStatus.Verified);
    }

    /// <summary>Any non-Blocked state → Rejected. The reason is surfaced by the caller (not persisted in the narrow core).</summary>
    public void Reject(string? reason = null)
    {
        Ensure(OnboardingStatus is not ProviderSubMerchantOnboardingStatus.Blocked);
        _ = reason;   // reserved: FE review flow / audit log (not stored)
        Transition(ProviderSubMerchantOnboardingStatus.Rejected);
    }

    /// <summary>{SubMerchantCreated, Verified} → Suspended (temporarily not split-eligible).</summary>
    public void Suspend()
    {
        Ensure(OnboardingStatus is ProviderSubMerchantOnboardingStatus.SubMerchantCreated
                                or ProviderSubMerchantOnboardingStatus.Verified);
        Transition(ProviderSubMerchantOnboardingStatus.Suspended);
    }

    /// <summary>Any non-Blocked state → Blocked (permanent).</summary>
    public void Block()
    {
        Ensure(OnboardingStatus is not ProviderSubMerchantOnboardingStatus.Blocked);
        Transition(ProviderSubMerchantOnboardingStatus.Blocked);
    }

    /// <summary>Suspended → its prior active state (Verified if verified, else SubMerchantCreated if keyed, else DataSubmitted).</summary>
    public void Reactivate()
    {
        Ensure(OnboardingStatus is ProviderSubMerchantOnboardingStatus.Suspended);
        var restored = VerifiedAt is not null
            ? ProviderSubMerchantOnboardingStatus.Verified
            : !string.IsNullOrWhiteSpace(SubMerchantKey)
                ? ProviderSubMerchantOnboardingStatus.SubMerchantCreated
                : ProviderSubMerchantOnboardingStatus.DataSubmitted;
        Transition(restored);
    }

    public void UpdateIban(string ibanEncrypted, string ibanLast4)
    {
        IbanEncrypted = ibanEncrypted;
        IbanLast4     = ibanLast4;
    }

    /// <summary>Profile edit that invalidates verification → back to DataSubmitted (re-verify), VerifiedAt cleared.</summary>
    public void UpdateProfileAndResetVerification(string ibanEncrypted, string ibanLast4, string? legalName, string? taxNumber)
    {
        IbanEncrypted = ibanEncrypted;
        IbanLast4     = ibanLast4;
        LegalName     = legalName;
        TaxNumber     = taxNumber;
        VerifiedAt    = null;
        Transition(ProviderSubMerchantOnboardingStatus.DataSubmitted);
    }

    // ── Internals ────────────────────────────────────────────────────────────────

    private static void Ensure(bool allowed)
    {
        if (!allowed)
            throw new AizenBusinessException((int)PaymentErrorCode.ProviderSubMerchantInvalidTransition);
    }

    /// <summary>Advances the onboarding state and mirrors the legacy display string.</summary>
    private void Transition(ProviderSubMerchantOnboardingStatus next)
    {
        OnboardingStatus = next;
        Status = next switch
        {
            ProviderSubMerchantOnboardingStatus.Suspended => "OnHold",
            ProviderSubMerchantOnboardingStatus.DataSubmitted => "OnHold",
            ProviderSubMerchantOnboardingStatus.Blocked   => "Blocked",
            ProviderSubMerchantOnboardingStatus.Rejected  => "Blocked",
            _                                             => "Active",   // NotStarted / SubMerchantCreated / Verified
        };
    }
}
