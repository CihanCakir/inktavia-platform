using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetPaymentDashboardKpis;

// FIX_DASHBOARD_KPI_PERIOD
[DocumentationInfo("GetPaymentDashboardKpisQueryHandler",
    "Computes payment admin dashboard KPI strip. Gross volume and platform commission are " +
    "scoped to the requested window (1W/1M/1Y) and summed across all charged statuses " +
    "(Captured, Released, PartiallyRefunded, Refunded, Disputed) - not today-only/Captured-only. " +
    "Change % compares against the immediately preceding equal-length window. " +
    "Payout-pending total and held-in-escrow are point-in-time balances (period-independent).")]
public sealed class GetPaymentDashboardKpisQueryHandler
    : AizenQueryHandler<GetPaymentDashboardKpisQuery, PaymentDashboardKpisDto>
{
    // Statuses that represent money actually collected from the payer. PendingIntent (not yet
    // collected), Failed and Cancelled are excluded from volume/commission.
    private static readonly PaymentTransactionStatus[] ChargedStatuses =
    {
        PaymentTransactionStatus.Captured,
        PaymentTransactionStatus.Released,
        PaymentTransactionStatus.PartiallyRefunded,
        PaymentTransactionStatus.Refunded,
        PaymentTransactionStatus.Disputed,
    };

    private readonly IPaymentTransactionRepository _transactions;
    private readonly IPayoutRecordRepository       _payouts;

    public GetPaymentDashboardKpisQueryHandler(
        IPaymentTransactionRepository transactions,
        IPayoutRecordRepository       payouts)
    {
        _transactions = transactions;
        _payouts      = payouts;
    }

    /// <summary>Maps the FE selector value to a window length in days. Default 30 (1M).</summary>
    private static int ResolveWindowDays(string? period) => (period ?? string.Empty).Trim().ToUpperInvariant() switch
    {
        "1W" => 7,
        "1Y" => 365,
        _    => 30, // "1M" and any unknown/empty value
    };

    public override async Task<PaymentDashboardKpisDto?> Handle(
        GetPaymentDashboardKpisQuery request, CancellationToken ct)
    {
        var now         = DateTime.UtcNow;
        var windowDays  = ResolveWindowDays(request.Period);
        // .SpecifyKind(Utc) so the timestamptz CreateDate filter is written with an explicit UTC kind.
        var periodStart = DateTime.SpecifyKind(now.AddDays(-windowDays),     DateTimeKind.Utc);
        var prevStart   = DateTime.SpecifyKind(now.AddDays(-windowDays * 2), DateTimeKind.Utc);
        var prevEnd     = periodStart;

        // Gross volume + commission over the selected window, summed server-side across charged statuses.
        var current  = await _transactions.GetVolumeAggregateAsync(ChargedStatuses, periodStart, now,     ct);
        var previous = await _transactions.GetVolumeAggregateAsync(ChargedStatuses, prevStart,   prevEnd, ct);

        decimal grossChange = previous.Gross > 0
            ? Math.Round((current.Gross - previous.Gross) / previous.Gross * 100m, 1)
            : 0m;
        decimal commChange = previous.Commission > 0
            ? Math.Round((current.Commission - previous.Commission) / previous.Commission * 100m, 1)
            : 0m;

        // -- Point-in-time balances / counts (not period-scoped -- these are "as of now") --

        // Pending transactions (PendingIntent) -- count only.
        var (_, pendingCount) = await _transactions.GetPagedAsync(
            PaymentTransactionStatus.PendingIntent, null, null,
            null, null, null, 0, 1, ct);

        // Failed transactions -- count only.
        var (_, failedCount) = await _transactions.GetPagedAsync(
            PaymentTransactionStatus.Failed, null, null,
            null, null, null, 0, 1, ct);

        // Held escrow (Captured SR transactions = money currently held, not yet released).
        var (escrowTx, _) = await _transactions.GetPagedAsync(
            PaymentTransactionStatus.Captured, TransactionType.ServiceRequestEscrow, null,
            null, null, null, 0, 10_000, ct);

        // Pending provider payouts outstanding.
        var (pendingPayouts, _) = await _payouts.GetPagedAsync(
            PayoutStatus.Pending, null, null, null, 0, 10_000, ct);

        decimal payoutTotal = pendingPayouts.Sum(p => p.Amount);
        decimal escrowTotal = escrowTx.Sum(t => t.NetPayoutAmount);

        return new PaymentDashboardKpisDto(
            GrossVolume:              current.Gross,
            GrossVolumeChange:        grossChange,
            PlatformCommission:       current.Commission,
            CommissionChange:         commChange,
            PayoutPendingTotal:       payoutTotal,
            PayoutChange:             0m,   // would need prior-period payout tracking
            HeldInEscrow:             escrowTotal,
            EscrowChange:             0m,
            PendingTransactionCount:  pendingCount,
            FailedTransactionCount:   failedCount
        );
    }
}
