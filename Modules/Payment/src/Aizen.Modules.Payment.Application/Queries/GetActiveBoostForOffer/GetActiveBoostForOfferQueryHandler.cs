using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetActiveBoostForOffer;

[DocumentationInfo("GetActiveBoostForOfferQueryHandler (BE-P11)",
    "Read model for the ranking/visibility consumer: the currently-live Active boost entitlement for an offer, or IsBoosted=false. Never touches commission (§19.4).")]
public sealed class GetActiveBoostForOfferQueryHandler
    : AizenQueryHandler<GetActiveBoostForOfferQuery, ActiveBoostResult>
{
    private readonly IPremiumEntitlementRepository _entitlements;

    public GetActiveBoostForOfferQueryHandler(IPremiumEntitlementRepository entitlements) => _entitlements = entitlements;

    public override async Task<ActiveBoostResult?> Handle(
        GetActiveBoostForOfferQuery request, CancellationToken ct)
    {
        var atUtc = request.AtUtc ?? DateTime.UtcNow;
        var e = await _entitlements.GetActiveByOfferAsync(request.OfferId, atUtc, ct);

        if (e is null)
            return new ActiveBoostResult(false, null, null, null, null, null, null);

        return new ActiveBoostResult(
            IsBoosted:         true,
            EntitlementId:     e.Id,
            ProviderProfileId: e.ProviderProfileId,
            ProductCode:       e.ProductCodeSnapshot,
            Status:            e.Status,
            StartsAt:          e.StartsAt,
            ExpiresAt:         e.ExpiresAt);
    }
}
