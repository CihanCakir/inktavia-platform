using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.Premium;
using FluentAssertions;

namespace Aizen.Modules.Payment.Domain.UnitTests.Premium;

/// <summary>
/// BE-P11 §9.1/§9.2/§13.9 — pure domain: point-in-time price resolution (single-active + overlap→conflict + gap guard),
/// price snapshot, and the guarded entitlement transitions (Activate/Revoke/Expire, ≤1 semantics).
/// </summary>
public sealed class PremiumDomainTests
{
    private static readonly DateTime T0 = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static PremiumProductPriceEntity Price(decimal amount, DateTime from, DateTime? to, long id = 1)
    {
        var e = PremiumProductPriceEntity.Create(1, amount, "TRY", from, to, priceCode: $"PP{id}");
        e.Id = id;
        return e;
    }

    // ── Point-in-time resolution: single active over [from,to) ──────────────────

    [Fact]
    public void Resolve_PicksTheSingleActivePriceCoveringTheInstant()
    {
        var older = Price(99.90m, T0, T0.AddMonths(3), id: 1);     // [Jan, Apr)
        var newer = Price(149.90m, T0.AddMonths(3), null, id: 2);  // [Apr, ∞)

        PremiumProductPriceResolver.Resolve(new[] { older, newer }, T0.AddMonths(1))!.PriceAmount.Should().Be(99.90m);
        PremiumProductPriceResolver.Resolve(new[] { older, newer }, T0.AddMonths(5))!.PriceAmount.Should().Be(149.90m);
        // Half-open: the boundary belongs to the next record.
        PremiumProductPriceResolver.Resolve(new[] { older, newer }, T0.AddMonths(3))!.PriceAmount.Should().Be(149.90m);
    }

    [Fact]
    public void Resolve_NoMatch_ReturnsNull()
        => PremiumProductPriceResolver.Resolve(new[] { Price(99.90m, T0.AddMonths(1), T0.AddMonths(2)) }, T0)
            .Should().BeNull();

    [Fact]
    public void Resolve_OverlappingActivePrices_Throws()
    {
        var a = Price(99.90m, T0, T0.AddMonths(6), id: 1);
        var b = Price(149.90m, T0.AddMonths(3), T0.AddMonths(9), id: 2);   // overlaps a on [Apr, Jul)
        var act = () => PremiumProductPriceResolver.Resolve(new[] { a, b }, T0.AddMonths(4));
        act.Should().Throw<AizenBusinessException>();
    }

    [Fact]
    public void ValidateInsertable_GapInChain_IsFlagged()
    {
        var a = Price(99.90m, T0, T0.AddMonths(3), id: 1);
        var b = Price(149.90m, T0.AddMonths(4), null, id: 2);      // Apr gap (a ends Mar-end, b starts +1mo)
        PremiumProductPriceResolver.ValidateInsertable(b, new[] { a }).Outcome.Should().Be(PremiumPriceGuardOutcome.Gap);
    }

    [Fact]
    public void ValidateInsertable_ContiguousChain_IsOk()
    {
        var a = Price(99.90m, T0, T0.AddMonths(3), id: 1);
        var b = Price(149.90m, T0.AddMonths(3), null, id: 2);     // contiguous
        PremiumProductPriceResolver.ValidateInsertable(b, new[] { a }).Outcome.Should().Be(PremiumPriceGuardOutcome.Ok);
    }

    // ── Price snapshot: the purchase captures the resolved price, immune to later changes ──

    [Fact]
    public void Purchase_SnapshotsResolvedPrice()
    {
        var purchase = PremiumPurchaseEntity.Create(
            providerProfileId: 7, premiumProductId: 1, productCodeSnapshot: "OFFER_BOOST_7D",
            premiumProductPriceIdSnapshot: 55, unitPriceSnapshot: 99.90m, currencyCodeSnapshot: "TRY",
            durationDaysSnapshot: 7, contextRef: 42, purchaseCode: "BST-1");

        purchase.Status.Should().Be(PremiumPurchaseStatus.Pending);
        purchase.UnitPriceSnapshot.Should().Be(99.90m);           // a later price change never mutates this
        purchase.DurationDaysSnapshot.Should().Be(7);
        purchase.ContextRef.Should().Be(42);
    }

    // ── Purchase lifecycle guards ───────────────────────────────────────────────

    [Fact]
    public void Purchase_MarkPaid_IsIdempotent_And_RefundRequiresPaid()
    {
        var p = PremiumPurchaseEntity.Create(7, 1, "OFFER_BOOST_7D", 55, 99.90m, "TRY", 7, 42, "BST-1");

        var refundBeforePaid = () => p.MarkRefunded();
        refundBeforePaid.Should().Throw<AizenBusinessException>("cannot refund a purchase that was never Paid");

        p.MarkPaid();
        p.MarkPaid();                                              // idempotent
        p.Status.Should().Be(PremiumPurchaseStatus.Paid);

        p.MarkRefunded();
        p.MarkRefunded();                                         // idempotent
        p.Status.Should().Be(PremiumPurchaseStatus.Refunded);
    }

    // ── Entitlement transitions (guarded) ───────────────────────────────────────

    [Fact]
    public void Entitlement_CreatedInactive_ActivatesOnce_ThenRevokes()
    {
        var e = PremiumEntitlementEntity.CreateInactive(10, 7, "OFFER_BOOST_7D", 42);
        e.Status.Should().Be(PremiumEntitlementStatus.Inactive);

        e.Activate(T0, T0.AddDays(7));
        e.Status.Should().Be(PremiumEntitlementStatus.Active);
        e.IsCurrentlyActive(T0.AddDays(3)).Should().BeTrue();
        e.IsCurrentlyActive(T0.AddDays(8)).Should().BeFalse();    // past window

        e.Activate(T0, T0.AddDays(7));                            // idempotent (already Active)
        e.Status.Should().Be(PremiumEntitlementStatus.Active);

        e.Revoke("refund");
        e.Status.Should().Be(PremiumEntitlementStatus.Revoked);
        e.RevokedAt.Should().NotBeNull();
        e.Revoke("refund");                                      // idempotent
    }

    [Fact]
    public void Entitlement_Expire_OnlyFromActive_Idempotent()
    {
        var e = PremiumEntitlementEntity.CreateInactive(10, 7, "OFFER_BOOST_7D", 42);
        var expireInactive = () => e.Expire();
        expireInactive.Should().Throw<AizenBusinessException>();

        e.Activate(T0, T0.AddDays(7));
        e.Expire();
        e.Status.Should().Be(PremiumEntitlementStatus.Expired);
        e.Expire();                                              // idempotent
    }

    [Fact]
    public void Entitlement_CannotRevokeExpired()
    {
        var e = PremiumEntitlementEntity.CreateInactive(10, 7, "OFFER_BOOST_7D", 42);
        e.Activate(T0, T0.AddDays(7));
        e.Expire();
        var revokeExpired = () => e.Revoke("late");
        revokeExpired.Should().Throw<AizenBusinessException>();
    }
}
