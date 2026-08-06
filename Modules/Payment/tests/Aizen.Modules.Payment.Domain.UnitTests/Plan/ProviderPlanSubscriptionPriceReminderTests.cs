using Aizen.Modules.Payment.Domain.Entities.Subscription;
using FluentAssertions;

namespace Aizen.Modules.Payment.Domain.UnitTests.Plan;

/// <summary>
/// N1 (§7) — the per-subscription/per-price-version reminder marker. The daily job must notify a provider ONCE about a
/// given upcoming price change and never re-notify on subsequent days, yet re-arm when the price re-versions.
/// </summary>
public sealed class ProviderPlanSubscriptionPriceReminderTests
{
    private static ProviderPlanSubscriptionEntity NewSub()
        => ProviderPlanSubscriptionEntity.Create(
            providerProfileId: 10, providerPlanId: 20,
            paidAmount: 199m, currencyCode: "TRY",
            periodStart: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            periodEnd:   new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            autoRenew: true, paymentTransactionId: null, commissionRateAtSubscription: 0.10m);

    [Fact]
    public void Fresh_Subscription_Needs_Reminder()
    {
        var sub = NewSub();
        sub.NeedsPriceChangeReminder("2026-02-01T00:00:00.0000000Z|249").Should().BeTrue();
    }

    [Fact]
    public void After_Marking_The_Same_Version_Does_Not_Re_Notify()
    {
        var sub = NewSub();
        const string version = "2026-02-01T00:00:00.0000000Z|249";

        sub.MarkPriceChangeReminderSent(version);

        // Next day's job run sees the same version — must NOT re-notify (idempotent).
        sub.NeedsPriceChangeReminder(version).Should().BeFalse();
    }

    [Fact]
    public void A_New_Price_Version_Re_Arms_The_Reminder()
    {
        var sub = NewSub();
        sub.MarkPriceChangeReminderSent("2026-02-01T00:00:00.0000000Z|249");

        // Price re-versioned (new amount) or renewal advanced (new date) → distinct key → notify again.
        sub.NeedsPriceChangeReminder("2026-02-01T00:00:00.0000000Z|279").Should().BeTrue();
        sub.NeedsPriceChangeReminder("2026-03-01T00:00:00.0000000Z|249").Should().BeTrue();
    }
}
