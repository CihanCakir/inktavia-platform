using Aizen.Modules.Payment.Domain.Entities.Plan;
using Aizen.Modules.Payment.Domain.Entities.Subscription;
using Aizen.Modules.Payment.Repository.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace Aizen.Modules.Payment.Repository.Seed;

/// <summary>
/// Seeds Provider and Participant subscription records for admin panel QA.
///
/// Plans (seeded by PaymentPlanSeed, IDs assigned by EF):
///   ProviderPlan  FREE=1, STANDARD=2, PREMIUM_PARTNER=3
///   ParticipantPlan BASIC=1, GOLD=2, PLATINUM=3
///
/// Provider subscriptions:
///   11011 Marina Ops        → STANDARD   (Active, current month)
///   11012 Teknik Servis     → PREMIUM_PARTNER (Active, current month)
///   11013 CargoDry Ekip     → FREE (no sub record needed; Free plan has PaidAmount=0)
///   11011 Marina Ops        → previous STANDARD record (Expired — last month)
///   11012 Teknik Servis     → previous STANDARD record (Expired — 2 months ago)
///
/// Participant subscriptions:
///   11003 Ayşe Demir        → GOLD      (Active)
///   11004 Mehmet Kaya       → PLATINUM  (Active)
///   11005 Deniz Yılmaz      → GOLD      (PastDue — payment failed)
///   11006 Selin Uzun        → BASIC     (Cancelled)
///   11007 Burak Arslan      → PLATINUM  (Expired — 3 months ago)
///
/// Idempotent — skipped if any provider subscription already exists.
/// </summary>
public sealed class SubscriptionMockSeed
{
    private readonly PaymentDbContext _db;
    private readonly ILogger<SubscriptionMockSeed> _logger;

    public SubscriptionMockSeed(PaymentDbContext db, ILogger<SubscriptionMockSeed> logger)
    {
        _db     = db;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await SeedProvider2IfNeededAsync(ct);

        if (await _db.ProviderSubscriptions.AnyAsync(ct))
        {
            _logger.LogDebug("Subscription mock seed skipped — data already present.");
            return;
        }

        var utcNow    = DateTime.UtcNow;
        var thisMonth = new DateTime(utcNow.Year, utcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var nextMonth = thisMonth.AddMonths(1);
        var lastMonth = thisMonth.AddMonths(-1);
        var twoMonths = thisMonth.AddMonths(-2);
        var threeMonths = thisMonth.AddMonths(-3);

        // Resolve Plan PKs from DB (PaymentPlanSeed must have run first)
        var providerPlanStandard = await _db.ProviderPlans
            .Where(p => p.PlanCode == "STANDARD").Select(p => p.Id).FirstOrDefaultAsync(ct);
        var providerPlanPremium = await _db.ProviderPlans
            .Where(p => p.PlanCode == "PREMIUM_PARTNER").Select(p => p.Id).FirstOrDefaultAsync(ct);
        var participantPlanBasic = await _db.ParticipantPlans
            .Where(p => p.PlanCode == "BASIC").Select(p => p.Id).FirstOrDefaultAsync(ct);
        var participantPlanGold = await _db.ParticipantPlans
            .Where(p => p.PlanCode == "GOLD").Select(p => p.Id).FirstOrDefaultAsync(ct);
        var participantPlanPlatinum = await _db.ParticipantPlans
            .Where(p => p.PlanCode == "PLATINUM").Select(p => p.Id).FirstOrDefaultAsync(ct);

        if (providerPlanStandard == 0 || participantPlanGold == 0)
        {
            _logger.LogWarning("SubscriptionMockSeed: plan records not found — run PaymentPlanSeed first.");
            return;
        }

        // ── Provider Subscriptions ────────────────────────────────────────────

        var providerSubs = new List<ProviderPlanSubscriptionEntity>();

        // 11011 Marina Ops — STANDARD — Active (current month)
        var ps1 = ProviderPlanSubscriptionEntity.Create(
            providerProfileId:            11011,
            providerPlanId:               providerPlanStandard,
            paidAmount:                   499m,
            currencyCode:                 "TRY",
            periodStart:                  thisMonth,
            periodEnd:                    nextMonth,
            autoRenew:                    true,
            paymentTransactionId:         null,   // linked by PaymentTransactionMockSeed (tx12) — not cross-referenced by ID in seed
            commissionRateAtSubscription: 0.12m);
        providerSubs.Add(ps1);

        // 11012 Teknik Servis — PREMIUM_PARTNER — Active (current month)
        var ps2 = ProviderPlanSubscriptionEntity.Create(
            providerProfileId:            11012,
            providerPlanId:               providerPlanPremium,
            paidAmount:                   999m,
            currencyCode:                 "TRY",
            periodStart:                  thisMonth,
            periodEnd:                    nextMonth,
            autoRenew:                    true,
            paymentTransactionId:         null,
            commissionRateAtSubscription: 0.08m);
        providerSubs.Add(ps2);

        // 11011 Marina Ops — STANDARD — Expired last month
        var ps3 = ProviderPlanSubscriptionEntity.Create(
            providerProfileId:            11011,
            providerPlanId:               providerPlanStandard,
            paidAmount:                   499m,
            currencyCode:                 "TRY",
            periodStart:                  lastMonth,
            periodEnd:                    thisMonth,
            autoRenew:                    true,
            paymentTransactionId:         null,
            commissionRateAtSubscription: 0.12m);
        ps3.MarkExpired();
        providerSubs.Add(ps3);

        // 11012 Teknik Servis — STANDARD — Expired 2 months ago (was on STANDARD before upgrading)
        var ps4 = ProviderPlanSubscriptionEntity.Create(
            providerProfileId:            11012,
            providerPlanId:               providerPlanStandard,
            paidAmount:                   499m,
            currencyCode:                 "TRY",
            periodStart:                  twoMonths,
            periodEnd:                    lastMonth,
            autoRenew:                    false,
            paymentTransactionId:         null,
            commissionRateAtSubscription: 0.12m);
        ps4.MarkExpired();
        providerSubs.Add(ps4);

        await _db.ProviderSubscriptions.AddRangeAsync(providerSubs, ct);

        // ── Participant Subscriptions ─────────────────────────────────────────

        var participantSubs = new List<ParticipantPlanSubscriptionEntity>();

        // 11003 Ayşe Demir — GOLD — Active
        var pp1 = ParticipantPlanSubscriptionEntity.Create(
            participantProfileId:          11003,
            participantPlanId:             participantPlanGold,
            paidAmount:                    199m,
            currencyCode:                  "TRY",
            periodStart:                   thisMonth,
            periodEnd:                     nextMonth,
            autoRenew:                     true,
            paymentTransactionId:          null,
            serviceDiscountAtSubscription: 0.05m,
            earnMultiplierAtSubscription:  1.5m);
        participantSubs.Add(pp1);

        // 11004 Mehmet Kaya — PLATINUM — Active
        var pp2 = ParticipantPlanSubscriptionEntity.Create(
            participantProfileId:          11004,
            participantPlanId:             participantPlanPlatinum,
            paidAmount:                    399m,
            currencyCode:                  "TRY",
            periodStart:                   thisMonth,
            periodEnd:                     nextMonth,
            autoRenew:                     true,
            paymentTransactionId:          null,
            serviceDiscountAtSubscription: 0.10m,
            earnMultiplierAtSubscription:  2.0m);
        participantSubs.Add(pp2);

        // 11005 Deniz Yılmaz — GOLD — PastDue (renewal failed)
        var pp3 = ParticipantPlanSubscriptionEntity.Create(
            participantProfileId:          11005,
            participantPlanId:             participantPlanGold,
            paidAmount:                    0m,   // payment failed — 0 collected this cycle
            currencyCode:                  "TRY",
            periodStart:                   thisMonth,
            periodEnd:                     nextMonth,
            autoRenew:                     true,
            paymentTransactionId:          null,
            serviceDiscountAtSubscription: 0.05m,
            earnMultiplierAtSubscription:  1.5m);
        pp3.MarkPastDue();
        participantSubs.Add(pp3);

        // 11006 Selin Uzun — BASIC — Cancelled
        var pp4 = ParticipantPlanSubscriptionEntity.Create(
            participantProfileId:          11006,
            participantPlanId:             participantPlanBasic,
            paidAmount:                    0m,
            currencyCode:                  "TRY",
            periodStart:                   lastMonth,
            periodEnd:                     thisMonth,
            autoRenew:                     false,
            paymentTransactionId:          null,
            serviceDiscountAtSubscription: 0m,
            earnMultiplierAtSubscription:  1.0m);
        pp4.Cancel("User requested cancellation via app settings.");
        participantSubs.Add(pp4);

        // 11007 Burak Arslan — PLATINUM — Expired 3 months ago
        var pp5 = ParticipantPlanSubscriptionEntity.Create(
            participantProfileId:          11007,
            participantPlanId:             participantPlanPlatinum,
            paidAmount:                    399m,
            currencyCode:                  "TRY",
            periodStart:                   threeMonths,
            periodEnd:                     twoMonths,
            autoRenew:                     false,
            paymentTransactionId:          null,
            serviceDiscountAtSubscription: 0.10m,
            earnMultiplierAtSubscription:  2.0m);
        pp5.MarkExpired();
        participantSubs.Add(pp5);

        await _db.ParticipantSubscriptions.AddRangeAsync(participantSubs, ct);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Subscription mock seed complete: {Provider} provider subs, {Participant} participant subs.",
            providerSubs.Count, participantSubs.Count);
    }

    private async Task SeedProvider2IfNeededAsync(CancellationToken ct)
    {
        const long provider2 = 100011;
        if (await _db.ProviderSubscriptions.AnyAsync(s => s.ProviderProfileId == provider2, ct))
        {
            _logger.LogDebug("Provider2 subscription seed skipped — data already present.");
            return;
        }

        var standardPlan = await _db.ProviderPlans
            .Where(p => p.PlanCode == "STANDARD")
            .FirstOrDefaultAsync(ct);

        if (standardPlan is null)
        {
            _logger.LogWarning("Provider2 subscription seed: STANDARD plan not found — run PaymentPlanSeed first.");
            return;
        }

        var utcNow = DateTime.UtcNow;
        var sub = ProviderPlanSubscriptionEntity.Create(
            providerProfileId:            provider2,
            providerPlanId:               standardPlan.Id,
            paidAmount:                   499m,
            currencyCode:                 "TRY",
            periodStart:                  utcNow,
            periodEnd:                    utcNow.AddDays(30),
            autoRenew:                    true,
            paymentTransactionId:         null,
            commissionRateAtSubscription: 0.12m);

        await _db.ProviderSubscriptions.AddAsync(sub, ct);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Provider2 subscription seed complete: STANDARD plan, active for 30 days.");
    }
}
