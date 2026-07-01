using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetPaymentDashboardKpis;

[DocumentationInfo("GetPaymentDashboardKpisQueryHandler",
    "Computes payment admin dashboard KPI strip: gross volume today vs yesterday, " +
    "commission today, pending payout total, held-in-escrow balance, " +
    "pending and failed transaction counts.")]
public sealed class GetPaymentDashboardKpisQueryHandler
    : AizenQueryHandler<GetPaymentDashboardKpisQuery, PaymentDashboardKpisDto>
{
    private readonly IPaymentTransactionRepository _transactions;
    private readonly IPayoutRecordRepository       _payouts;

    public GetPaymentDashboardKpisQueryHandler(
        IPaymentTransactionRepository transactions,
        IPayoutRecordRepository       payouts)
    {
        _transactions = transactions;
        _payouts      = payouts;
    }

    public override async Task<PaymentDashboardKpisDto?> Handle(
        GetPaymentDashboardKpisQuery request, CancellationToken ct)
    {
        var now           = DateTime.UtcNow;
        var todayStart    = now.Date;
        var todayEnd      = todayStart.AddDays(1);
        var yesterdayStart = todayStart.AddDays(-1);
        var yesterdayEnd  = todayStart;

        // Today's captured transactions
        var (todayTx, _) = await _transactions.GetPagedAsync(
            PaymentTransactionStatus.Captured, null, null,
            todayStart, todayEnd, null, 0, 10_000, ct);

        // Yesterday's captured transactions (for change %)
        var (yesterdayTx, _) = await _transactions.GetPagedAsync(
            PaymentTransactionStatus.Captured, null, null,
            yesterdayStart, yesterdayEnd, null, 0, 10_000, ct);

        // Pending transactions (PendingIntent)
        var (pendingTx, pendingCount) = await _transactions.GetPagedAsync(
            PaymentTransactionStatus.PendingIntent, null, null,
            null, null, null, 0, 1, ct);

        // Failed transactions
        var (_, failedCount) = await _transactions.GetPagedAsync(
            PaymentTransactionStatus.Failed, null, null,
            null, null, null, 0, 1, ct);

        // Held escrow (Captured SR transactions = money is held)
        var (escrowTx, _) = await _transactions.GetPagedAsync(
            PaymentTransactionStatus.Captured, TransactionType.ServiceRequestEscrow, null,
            null, null, null, 0, 10_000, ct);

        var (pendingPayouts, _) = await _payouts.GetPagedAsync(
            PayoutStatus.Pending, null, null, null, 0, 10_000, ct);

        decimal grossToday    = todayTx.Sum(t => t.GrossAmount);
        decimal grossYesterday = yesterdayTx.Sum(t => t.GrossAmount);
        decimal grossChange   = grossYesterday > 0
            ? Math.Round((grossToday - grossYesterday) / grossYesterday * 100m, 1)
            : 0m;

        decimal commToday    = todayTx.Sum(t => t.CommissionAmount + t.VatOnCommission);
        decimal commYesterday = yesterdayTx.Sum(t => t.CommissionAmount + t.VatOnCommission);
        decimal commChange   = commYesterday > 0
            ? Math.Round((commToday - commYesterday) / commYesterday * 100m, 1)
            : 0m;

        decimal payoutTotal  = pendingPayouts.Sum(p => p.Amount);
        decimal escrowTotal  = escrowTx.Sum(t => t.NetPayoutAmount);

        return new PaymentDashboardKpisDto(
            GrossVolumeToday:         grossToday,
            GrossVolumeChange:        grossChange,
            PlatformCommissionToday:  commToday,
            CommissionChange:         commChange,
            PayoutPendingTotal:       payoutTotal,
            PayoutChange:             0m,   // would need prior period payout tracking
            HeldInEscrow:             escrowTotal,
            EscrowChange:             0m,
            PendingTransactionCount:  pendingCount,
            FailedTransactionCount:   failedCount
        );
    }
}
