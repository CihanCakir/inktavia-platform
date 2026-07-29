using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Application.Queries.GetActiveBoostForOffer;

/// <summary>
/// BE-P11 §13.9 — read model for the ranking/visibility consumer (listing/GeoDiscovery later). Returns the currently-live
/// Active boost entitlement for an offer, or null. P11 only exposes the entitlement state; it does NOT implement ranking.
/// </summary>
public sealed class GetActiveBoostForOfferQuery : AizenQuery<ActiveBoostResult>
{
    public required long     OfferId { get; init; }
    public          DateTime? AtUtc  { get; init; }
}

public sealed record ActiveBoostResult(
    bool                     IsBoosted,
    long?                    EntitlementId,
    long?                    ProviderProfileId,
    string?                  ProductCode,
    PremiumEntitlementStatus? Status,
    DateTime?                StartsAt,
    DateTime?                ExpiresAt);
