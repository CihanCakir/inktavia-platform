using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Application.Queries.GetPaymentDashboardKpis;
using Aizen.Modules.Payment.Domain.Entities.Transaction;
using Aizen.Modules.Payment.Repository.Persistence;
using Aizen.Modules.Payment.Repository.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Aizen.Modules.Payment.Repository.UnitTests;

/// <summary>
/// FIX_DASHBOARD_KPI_PERIOD — the payment dashboard KPI strip is period-scoped (1W/1M/1Y) and sums gross/commission
/// across all charged statuses (Captured, Released, PartiallyRefunded, Refunded, Disputed) instead of today-only /
/// Captured-only. Point-in-time balances/counts are unaffected. Verified over the real PaymentTransactionRepository +
/// PaymentDbContext: (1) the handler window/status behaviour; (2) the new server-side GetVolumeAggregateAsync.
/// </summary>
public sealed class PaymentDashboardKpiPeriodTests
{
    private static PaymentDbContext NewDb()
        => new(new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase($"kpi-period-{Guid.NewGuid():N}").Options);

    private static readonly DateTime Now = DateTime.UtcNow;

    // Charged rows land inside the 1W window (2 days old) except the "June" one at 100 days (1Y only).
    private static PaymentTransactionEntity Tx(
        PaymentTransactionStatus status, decimal gross, decimal commission, decimal vat, int daysAgo) =>
        Backdate(
            PaymentTransactionEntity.Create(
                transactionCode: $"TXN-{Guid.NewGuid():N}"[..20], transactionType: TransactionType.ServiceRequestEscrow,
                contextType: TransactionContextType.ServiceRequest, contextId: 1, contextSubId: 1,
                payerProfileId: 100, recipientProfileId: 200, grossAmount: gross,
                commissionAmount: commission, commissionRateSnapshot: 0.12m, vatOnCommission: vat, netPayoutAmount: gross - commission - vat,
                discountAmount: 0m, currencyCode: "TRY", gatewayProvider: "iyzico",
                idempotencyKey: $"SR-{Guid.NewGuid():N}", escrowRequired: true),
            status, Now.AddDays(-daysAgo));

    private static PaymentTransactionEntity Backdate(PaymentTransactionEntity tx, PaymentTransactionStatus status, DateTime createDate)
    {
        // Drive the entity to the target status through its transitions, then stamp the audit CreateDate the KPI query filters on.
        if (status != PaymentTransactionStatus.PendingIntent && status != PaymentTransactionStatus.Failed
            && status != PaymentTransactionStatus.Cancelled)
            tx.Capture("REF");                 // PendingIntent → Captured
        if (status == PaymentTransactionStatus.Released) tx.Release();
        if (status == PaymentTransactionStatus.Refunded || status == PaymentTransactionStatus.PartiallyRefunded
            || status == PaymentTransactionStatus.Disputed)
            ForceStatus(tx, status);           // no clean domain path in this fixture — set directly
        if (status == PaymentTransactionStatus.Failed || status == PaymentTransactionStatus.Cancelled)
            ForceStatus(tx, status);
        tx.CreateDate = createDate;
        return tx;
    }

    // The status setter is private on the entity; for statuses without a simple transition path we set it via the backing property.
    private static void ForceStatus(PaymentTransactionEntity tx, PaymentTransactionStatus status)
    {
        typeof(PaymentTransactionEntity).GetProperty(nameof(PaymentTransactionEntity.Status))!
            .SetValue(tx, status);
    }

    private static GetPaymentDashboardKpisQueryHandler NewHandler(PaymentDbContext db)
        => new(new PaymentTransactionRepository(db), new PayoutRecordRepository(db));

    /// <summary>Seeds a mixed set of rows across statuses and ages. Charged-in-1W total: gross 3700 / commission 444.</summary>
    private static async Task SeedAsync(PaymentDbContext db)
    {
        db.Transactions.AddRange(
            Tx(PaymentTransactionStatus.Released,           gross: 1000m, commission: 100m, vat: 20m, daysAgo: 2),
            Tx(PaymentTransactionStatus.Captured,           gross: 2000m, commission: 200m, vat: 40m, daysAgo: 2),
            Tx(PaymentTransactionStatus.Disputed,           gross:  300m, commission:  30m, vat:  6m, daysAgo: 1),
            Tx(PaymentTransactionStatus.PartiallyRefunded,  gross:  400m, commission:  40m, vat:  8m, daysAgo: 1),
            Tx(PaymentTransactionStatus.Released,           gross: 5000m, commission: 500m, vat: 100m, daysAgo: 100), // "June" → 1Y only
            Tx(PaymentTransactionStatus.PendingIntent,      gross: 9999m, commission: 999m, vat: 99m, daysAgo: 2),   // excluded
            Tx(PaymentTransactionStatus.Failed,             gross: 8888m, commission: 888m, vat: 88m, daysAgo: 2),   // excluded
            Tx(PaymentTransactionStatus.Cancelled,          gross: 7777m, commission: 777m, vat: 77m, daysAgo: 2));  // excluded
        await db.SaveChangesAsync();
    }

    // ── Handler: window + status behaviour ──────────────────────────────────────

    [Fact]
    public async Task Handler_1W_Sums_ChargedRecent_Excludes_Pending_Failed_Cancelled_And_OlderThan7d()
    {
        await using var db = NewDb();
        await SeedAsync(db);

        var dto = await NewHandler(db).Handle(new GetPaymentDashboardKpisQuery { Period = "1W" }, default);

        dto.Should().NotBeNull();
        dto!.GrossVolume.Should().Be(3700m, "1000 Released + 2000 Captured + 300 Disputed + 400 PartiallyRefunded");
        dto.PlatformCommission.Should().Be(444m, "commission + VAT across the same charged rows");
        // 9999 PendingIntent, 8888 Failed, 7777 Cancelled and the 5000 June row are all excluded from 1W.
        dto.GrossVolume.Should().NotBe(3700m + 5000m);
    }

    [Fact]
    public async Task Handler_1Y_Includes_June_Released_Row_That_1W_Excludes()
    {
        await using var db = NewDb();
        await SeedAsync(db);
        var handler = NewHandler(db);

        var oneWeek = await handler.Handle(new GetPaymentDashboardKpisQuery { Period = "1W" }, default);
        var oneYear = await handler.Handle(new GetPaymentDashboardKpisQuery { Period = "1Y" }, default);

        oneWeek!.GrossVolume.Should().Be(3700m);
        oneYear!.GrossVolume.Should().Be(8700m, "1Y additionally includes the 5000 Released row from ~100 days ago");
        (oneYear.GrossVolume - oneWeek.GrossVolume).Should().Be(5000m);
    }

    [Fact]
    public async Task Handler_NullPeriod_BehavesAs_1M()
    {
        await using var db = NewDb();
        await SeedAsync(db);
        var handler = NewHandler(db);

        var nullPeriod = await handler.Handle(new GetPaymentDashboardKpisQuery { Period = null }, default);
        var oneMonth   = await handler.Handle(new GetPaymentDashboardKpisQuery { Period = "1M" }, default);

        // 1M (30d) covers the recent charged rows but not the 100-day June row.
        nullPeriod!.GrossVolume.Should().Be(3700m);
        nullPeriod.GrossVolume.Should().Be(oneMonth!.GrossVolume);
        nullPeriod.PlatformCommission.Should().Be(oneMonth.PlatformCommission);
    }

    // ── Repository: GetVolumeAggregateAsync ─────────────────────────────────────

    private static readonly PaymentTransactionStatus[] Charged =
    {
        PaymentTransactionStatus.Captured, PaymentTransactionStatus.Released,
        PaymentTransactionStatus.PartiallyRefunded, PaymentTransactionStatus.Refunded,
        PaymentTransactionStatus.Disputed,
    };

    [Fact]
    public async Task Aggregate_FiltersByStatus_And_SumsServerSide()
    {
        await using var db = NewDb();
        await SeedAsync(db);
        var repo = new PaymentTransactionRepository(db);

        var (gross, commission, count) = await repo.GetVolumeAggregateAsync(Charged, null, null, default);

        // All 5 charged rows (incl. the June one) regardless of date; excludes Pending/Failed/Cancelled.
        gross.Should().Be(1000m + 2000m + 300m + 400m + 5000m);
        commission.Should().Be(120m + 240m + 36m + 48m + 600m);
        count.Should().Be(5);
    }

    [Fact]
    public async Task Aggregate_DateWindow_Is_FromInclusive_ToExclusive()
    {
        await using var db = NewDb();
        var repo = new PaymentTransactionRepository(db);

        var at = new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc);
        var onFrom  = Backdate(NewCharged(100m, 10m, 2m), PaymentTransactionStatus.Captured, at);          // == from  → included
        var onTo    = Backdate(NewCharged(200m, 20m, 4m), PaymentTransactionStatus.Captured, at.AddDays(1)); // == to    → excluded
        db.Transactions.AddRange(onFrom, onTo);
        await db.SaveChangesAsync();

        var (gross, _, count) = await repo.GetVolumeAggregateAsync(Charged, at, at.AddDays(1), default);

        gross.Should().Be(100m, "the [from, to) window includes the row at `from` and excludes the row at `to`");
        count.Should().Be(1);
    }

    [Fact]
    public async Task Aggregate_NoMatchingRows_ReturnsZeros()
    {
        await using var db = NewDb();
        await SeedAsync(db);
        var repo = new PaymentTransactionRepository(db);

        // A window far in the future matches nothing → (0, 0, 0), not null.
        var (gross, commission, count) = await repo.GetVolumeAggregateAsync(
            Charged, Now.AddDays(1000), Now.AddDays(2000), default);

        gross.Should().Be(0m);
        commission.Should().Be(0m);
        count.Should().Be(0);
    }

    private static PaymentTransactionEntity NewCharged(decimal gross, decimal commission, decimal vat) =>
        PaymentTransactionEntity.Create(
            transactionCode: $"TXN-{Guid.NewGuid():N}"[..20], transactionType: TransactionType.ServiceRequestEscrow,
            contextType: TransactionContextType.ServiceRequest, contextId: 1, contextSubId: 1,
            payerProfileId: 100, recipientProfileId: 200, grossAmount: gross,
            commissionAmount: commission, commissionRateSnapshot: 0.12m, vatOnCommission: vat, netPayoutAmount: gross - commission - vat,
            discountAmount: 0m, currencyCode: "TRY", gatewayProvider: "iyzico",
            idempotencyKey: $"SR-{Guid.NewGuid():N}", escrowRequired: true);
}
