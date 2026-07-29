namespace Aizen.Modules.Payment.Abstraction.Dto;

/// <summary>
/// BE-P11 §13.9 — the active OFFER_BOOST_7D entitlement state for an offer (provider-facing). <c>IsBoosted</c> is false
/// (with the rest null) when no live boost exists. <c>Status</c> is the <c>PremiumEntitlementStatus</c> name.
/// </summary>
public sealed class OfferBoostStatusDto
{
    public bool      IsBoosted         { get; init; }
    public long?     EntitlementId     { get; init; }
    public long?     ProviderProfileId { get; init; }
    public string?   ProductCode       { get; init; }
    public string?   Status            { get; init; }
    public DateTime? StartsAt          { get; init; }
    public DateTime? ExpiresAt         { get; init; }
}
