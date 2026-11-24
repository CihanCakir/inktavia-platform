using Aizen.Core.CQRS.Message;

namespace Aizen.Modules.Payment.Application.Queries.GetPaymentDashboardKpis;

// FIX_DASHBOARD_KPI_PERIOD: gross/commission are now period-scoped (1W/1M/1Y) instead of
// today-only, and computed across all charged statuses instead of Captured-only.
public sealed class GetPaymentDashboardKpisQuery : AizenQuery<PaymentDashboardKpisDto>
{
    /// <summary>
    /// KPI window for the gross-volume and platform-commission figures.
    /// Accepts "1W" (7d), "1M" (30d) or "1Y" (365d); null/unknown falls back to "1M".
    /// Payout-pending and held-in-escrow remain point-in-time balances (period-independent).
    /// </summary>
    public string? Period { get; init; }
}

public sealed record PaymentDashboardKpisDto(
    decimal GrossVolume,
    decimal GrossVolumeChange,
    decimal PlatformCommission,
    decimal CommissionChange,
    decimal PayoutPendingTotal,
    decimal PayoutChange,
    decimal HeldInEscrow,
    decimal EscrowChange,
    int     PendingTransactionCount,
    int     FailedTransactionCount
);
