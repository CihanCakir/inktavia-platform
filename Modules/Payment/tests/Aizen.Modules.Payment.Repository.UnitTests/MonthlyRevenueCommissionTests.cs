using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Application.Queries.GetMonthlyRevenueCommissionReport;
using Aizen.Modules.Payment.Domain.Entities.Reporting;
using Aizen.Modules.Payment.Repository.Persistence;
using Aizen.Modules.Payment.Repository.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Payment.Repository.UnitTests;

/// <summary>
/// C1 — the monthly revenue/commission series over the append-only ledger (in-memory DbContext): 12 UTC month
/// buckets oldest→newest, revenue = Σ revenue-nature lines, commission = Σ ProviderCommissionRevenue lines, reversals
/// subtract, expenses excluded, and empty months are zero-filled (not dropped).
/// </summary>
public sealed class MonthlyRevenueCommissionTests
{
    private static PaymentDbContext NewDb()
        => new(new DbContextOptionsBuilder<PaymentDbContext>().UseInMemoryDatabase($"rev-{Guid.NewGuid():N}").Options);

    private static FinancialLedgerEntryEntity Entry(
        LedgerAccountLine line, decimal amount, DateTime occurredUtc, bool reversal = false, string currency = "TRY")
        => FinancialLedgerEntryEntity.Create(
            entryCode: $"E-{Guid.NewGuid():N}"[..20], accountLine: line, amount: amount, isReversal: reversal,
            currencyCode: currency, sourceType: LedgerSourceType.AcceptanceSnapshot, sourceRef: 1,
            transactionId: null, providerProfileId: null, customerProfileId: null,
            occurredAtUtc: occurredUtc, postedAtUtc: occurredUtc, note: null);

    [Fact]
    public async Task Twelve_utc_buckets_oldest_to_newest_with_sums_reversals_and_zero_fill()
    {
        await using var db = NewDb();
        var now = DateTime.UtcNow;
        var currentMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var thisMonth = currentMonthStart.AddDays(14);              // mid current month
        var twoMonthsAgo = currentMonthStart.AddMonths(-2).AddDays(9);

        db.FinancialLedgerEntries.AddRange(
            // Current month: revenue 600 (commission) + 145 (platform-fee net) = 745; commission = 600.
            Entry(LedgerAccountLine.ProviderCommissionRevenue, 600m, thisMonth),
            Entry(LedgerAccountLine.CustomerPlatformFeeNetRevenue, 145m, thisMonth),
            Entry(LedgerAccountLine.PaymentProcessingExpense, 50m, thisMonth),          // expense — excluded from revenue
            // Two months ago: commission 300 then a −100 reversal → revenue 200, commission 200.
            Entry(LedgerAccountLine.ProviderCommissionRevenue, 300m, twoMonthsAgo),
            Entry(LedgerAccountLine.ProviderCommissionRevenue, 100m, twoMonthsAgo, reversal: true),
            // A non-TRY revenue line in the current month must NOT be counted.
            Entry(LedgerAccountLine.ProviderCommissionRevenue, 999m, thisMonth, currency: "USD"));
        await db.SaveChangesAsync();

        var repo = new FinancialLedgerRepository(db);
        var points = await repo.GetMonthlyRevenueCommissionAsync(12, CancellationToken.None);

        points.Should().HaveCount(12);
        points.Select(p => p.Month).Should().BeInAscendingOrder();
        points[^1].Month.Should().Be(new DateOnly(now.Year, now.Month, 1), "the last bucket is the current UTC month");

        points[^1].Revenue.Should().Be(745m);        // 600 + 145 (expense + USD excluded)
        points[^1].Commission.Should().Be(600m);

        points[^3].Revenue.Should().Be(200m);         // 300 − 100 reversal
        points[^3].Commission.Should().Be(200m);

        points[^2].Revenue.Should().Be(0m, "the month between is empty → zero-filled, not dropped");
        points[^2].Commission.Should().Be(0m);
    }

    [Fact]
    public async Task Handler_maps_points_to_yyyy_MM_dtos()
    {
        await using var db = NewDb();
        var now = DateTime.UtcNow;
        db.FinancialLedgerEntries.Add(
            Entry(LedgerAccountLine.ProviderCommissionRevenue, 500m,
                new DateTime(now.Year, now.Month, 10, 0, 0, 0, DateTimeKind.Utc)));
        await db.SaveChangesAsync();

        var handler = new GetMonthlyRevenueCommissionReportQueryHandler(new FinancialLedgerRepository(db));
        var result = await handler.Handle(new GetMonthlyRevenueCommissionReportQuery { Months = 12 }, CancellationToken.None);

        result!.Should().HaveCount(12);
        result[^1].Month.Should().Be($"{now:yyyy-MM}");
        result[^1].Revenue.Should().Be(500m);
        result[^1].Commission.Should().Be(500m);
    }
}
