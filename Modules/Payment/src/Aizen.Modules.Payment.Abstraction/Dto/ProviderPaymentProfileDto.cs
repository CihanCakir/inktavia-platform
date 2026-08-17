namespace Aizen.Modules.Payment.Abstraction.Dto;

public sealed class ProviderPaymentProfileDto
{
    public string  GatewayProvider { get; init; } = default!;
    public bool    HasIban         { get; init; }
    public string? IbanMasked      { get; init; }
    public string? LegalName       { get; init; }
    public string? TaxNumberMasked { get; init; }
    public string  Status          { get; init; } = "OnHold";
    public DateTimeOffset? VerifiedAt { get; init; }

    // ── BE-I1: sub-merchant onboarding + split-eligibility (additive; existing fields unchanged) ──
    /// <summary>The single split gate signal (§21.4) — payments can only be routed to a split-eligible provider.</summary>
    public bool    IsSplitEligible  { get; init; }
    /// <summary>Onboarding lifecycle (NotStarted/DataSubmitted/SubMerchantCreated/Verified/Rejected/Suspended/Blocked).</summary>
    public string? OnboardingStatus { get; init; }
    /// <summary>PERSONAL / PRIVATE_COMPANY / LIMITED_OR_JOINT_STOCK_COMPANY — null until captured on the KYC form.</summary>
    public string? SubMerchantType  { get; init; }
    /// <summary>Masked iyzico sub-merchant key (last 4) once created; null before.</summary>
    public string? SubMerchantKeyMasked { get; init; }
    /// <summary>True when an IBAN is still required for eligibility (BE-P9-fix §5 — no IBAN ⇒ not split-eligible).</summary>
    public bool    IbanRequired     { get; init; }
    /// <summary>Populated when onboarding was rejected (admin reason), for the provider-facing banner.</summary>
    public string? RejectionReason  { get; init; }
}
