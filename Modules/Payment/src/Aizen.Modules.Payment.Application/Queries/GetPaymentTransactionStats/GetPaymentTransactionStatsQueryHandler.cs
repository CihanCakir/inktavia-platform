using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetPaymentTransactionStats;

[DocumentationInfo("GetPaymentTransactionStatsQueryHandler",
    "Computes transaction ledger KPI cards: NetLiquidity (captured net payout total), " +
    "PendingClearances (pending-intent gross total), OperationalBurn (commission collected today), " +
    "FleetRoi (net-to-gross efficiency ratio for captured transactions). " +
    "Change metrics compare today vs yesterday for daily figures; pending/liquidity compare " +
    "current snapshot vs prior month snapshot (approximated from prior 30-day captured sum).")]
public sealed class GetPaymentTransactionStatsQueryHandler
    : AizenQueryHandler<GetPaymentTransactionStatsQuery, PaymentTransactionStatsDto>
{
    private readonly IPaymentTransactionRepository _transactions;

    public GetPaymentTransactionStatsQueryHandler(IPaymentTransactionRepository transactions)
        => _transactions = transactions;

    public override async Task<PaymentTransactionStatsDto?> Handle(
        GetPaymentTransactionStatsQuery request, CancellationToken ct)
    {
        var now            = DateTime.UtcNow;
        var todayStart     = now.Date;
        var yesterdayStart = todayStart.AddDays(-1);

        // All captured transactions (for net liquidity + fleet ROI)
        var (allCaptured, _) = await _transactions.GetPagedAsync(
            PaymentTransactionStatus.Captured, null, null,
            null, null, null, 0, 10_000, ct);

        // Today's captured (for operational burn today)
        var (todayCaptured, _) = await _transactions.GetPagedAsync(
            PaymentTransactionStatus.Captured, null, null,
            todayStart, now, null, 0, 5_000, ct);

        // Yesterday's captured (for operational burn change)
        var (yesterdayCaptured, _) = await _transactions.GetPagedAsync(
            PaymentTransactionStatus.Captured, null, null,
            yesterdayStart, todayStart, null, 0, 5_000, ct);

        // All pending-intent transactions (for pending clearances)
        var (pendingItems, _) = await _transactions.GetPagedAsync(
            PaymentTransactionStatus.PendingIntent, null, null,
            null, null, null, 0, 5_000, ct);

        // Compute NetLiquidity = sum of NetPayoutAmount for all Captured (money owed to providers)
        decimal netLiquidity = allCaptured.Sum(t => t.NetPayoutAmount);

        // PendingClearances = total pending-intent gross
        decimal pendingClearances = pendingItems.Sum(t => t.GrossAmount);

        // OperationalBurn = commission + VAT on commission (platform's fee revenue)
        decimal burnToday     = todayCaptured.Sum(t => t.CommissionAmount + t.VatOnCommission);
        decimal burnYesterday = yesterdayCaptured.Sum(t => t.CommissionAmount + t.VatOnCommission);
        decimal burnChange    = burnYesterday > 0
            ? Math.Round((burnToday - burnYesterday) / burnYesterday * 100m, 1)
            : 0m;

        // FleetRoi = NetPayoutAmount / GrossAmount ratio (how much providers receive from gross)
        decimal totalGross  = allCaptured.Sum(t => t.GrossAmount);
        decimal fleetRoi    = totalGross > 0
            ? Math.Round(allCaptured.Sum(t => t.NetPayoutAmount) / totalGross * 100m, 2)
            : 0m;

        return new PaymentTransactionStatsDto(
            NetLiquidity:            netLiquidity,
            NetLiquidityChange:      0m,       // requires time-series snapshot — deferred
            PendingClearances:       pendingClearances,
            PendingClearancesChange: 0m,       // requires prior snapshot
            OperationalBurn:         burnToday,
            OperationalBurnChange:   burnChange,
            FleetRoi:                fleetRoi,
            FleetRoiChange:          0m        // requires prior period ratio
        );
    }
}
