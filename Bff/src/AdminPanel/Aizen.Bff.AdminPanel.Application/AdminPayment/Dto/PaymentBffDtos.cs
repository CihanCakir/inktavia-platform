using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Dto;

// ─── Transaction ──────────────────────────────────────────────────────────────

/// <summary>
/// BFF-side mirror of PaymentTransactionDto (Application layer).
/// Fields match the JSON serialized by PaymentTransactionController.GetById → return Ok(result).
/// BFF enriches PayerDisplayName and RecipientDisplayName from Identity bulk call.
/// </summary>
public sealed record PaymentTransactionBffDto(
    long                     TransactionId,
    string                   TransactionCode,
    TransactionType          TransactionType,
    PaymentTransactionStatus Status,
    long                     PayerProfileId,
    long?                    RecipientProfileId,
    decimal                  GrossAmount,
    decimal                  CommissionAmount,
    decimal                  CommissionRateSnapshot,
    decimal                  VatOnCommission,
    decimal                  NetPayoutAmount,
    decimal                  DiscountAmount,
    decimal                  TotalRefundedAmount,
    string                   CurrencyCode,
    string                   GatewayProvider,
    string?                  GatewayReference,
    bool                     EscrowRequired,
    DateTime?                CapturedAt,
    DateTime?                ReleasedAt,
    DateTime?                LastRefundedAt,
    DateTime?                CancelledAt,
    DateTime?                ReinstatedAt,
    DateTime?                CreateDate
)
{
    /// <summary>Display name of the payer — resolved from Identity module by BFF.</summary>
    public string? PayerDisplayName     { get; init; }
    /// <summary>Display name of the recipient — resolved from Identity module by BFF. Null for participant-initiated payments.</summary>
    public string? RecipientDisplayName { get; init; }
}

/// <summary>Paged transaction list response.</summary>
public sealed record PaymentTransactionListBffResult(
    List<PaymentTransactionBffDto> Items,
    int                            Total,
    int                            Page,
    int                            PageSize
);

// ─── Refund history ───────────────────────────────────────────────────────────

/// <summary>
/// BFF-side mirror of TransactionRefundRecordDto (Application layer).
/// </summary>
public sealed record TransactionRefundRecordBffDto(
    long                    Id,
    string                  RefundCode,
    RefundType              RefundType,
    RefundReason            Reason,
    TransactionRefundStatus Status,
    decimal                 Amount,
    string                  CurrencyCode,
    string?                 GatewayRefundReference,
    DateTime?               ProcessedAt,
    string?                 FailureReason,
    string?                 AdminNote,
    DateTime?               ReversedAt,
    string?                 ReversalReason,
    string?                 ReversalAdminNote,
    DateTime?               CreateDate
);

// ─── Payouts ──────────────────────────────────────────────────────────────────

/// <summary>BFF-side mirror of PendingPayoutDto (Application layer).
/// BFF enriches ProviderDisplayName from Identity bulk call.</summary>
public sealed record PendingPayoutBffDto(
    long     PayoutRecordId,
    long     TransactionId,
    long     ProviderProfileId,
    decimal  Amount,
    string   CurrencyCode,
    string?  GatewayPayoutId,
    DateTime RequestedAt
)
{
    /// <summary>Provider display name — resolved from Identity module by BFF.</summary>
    public string? ProviderDisplayName { get; init; }
    /// <summary>Provider avatar URL — resolved from Identity module by BFF.</summary>
    public string? ProviderAvatarUrl   { get; init; }
}

// ─── Plans ────────────────────────────────────────────────────────────────────

/// <summary>BFF-side mirror of ProviderPlanDto (Application layer).</summary>
public sealed record ProviderPlanBffDto(
    long    Id,
    string  PlanCode,
    string  Name,
    decimal MonthlyPriceTRY,
    int?    MaxActiveOffers,
    bool    HasPriorityBoost,
    bool    HasFullAnalytics,
    bool    IsFree,
    int     SortOrder
);

/// <summary>BFF-side mirror of ParticipantPlanDto (Application layer).</summary>
public sealed record ParticipantPlanBffDto(
    long    Id,
    string  PlanCode,
    string  Name,
    decimal MonthlyPriceTRY,
    decimal ServiceDiscountRate,
    decimal CargoDryDiscountRate,
    decimal InkCoinEarnMultiplier,
    int     SortOrder
);

// ─── Commission ───────────────────────────────────────────────────────────────

/// <summary>BFF-side mirror of CommissionRateResult (Application layer).</summary>
public sealed record CommissionRateBffResult(decimal Rate, string ResolvedFrom);

// ─── Dashboard KPIs (gap report) ─────────────────────────────────────────────

/// <summary>Payment admin dashboard KPI summary.</summary>
public sealed record PaymentDashboardKpisBffDto(
    decimal GrossVolumeToday,
    decimal GrossVolumeChange,
    decimal PlatformCommissionToday,
    decimal CommissionChange,
    decimal PayoutPendingTotal,
    decimal PayoutChange,
    decimal HeldInEscrow,
    decimal EscrowChange,
    int     PendingTransactionCount,
    int     FailedTransactionCount
);

// ─── Transaction Stats (gap report) ──────────────────────────────────────────

/// <summary>Transaction ledger KPI cards.</summary>
public sealed record PaymentTransactionStatsBffDto(
    decimal NetLiquidity,
    decimal NetLiquidityChange,
    decimal PendingClearances,
    decimal PendingClearancesChange,
    decimal OperationalBurn,
    decimal OperationalBurnChange,
    decimal FleetRoi,
    decimal FleetRoiChange
);

// ─── Payout extended (gap report) ────────────────────────────────────────────

/// <summary>Full payout record for provider-payouts admin list.</summary>
public sealed record PaymentPayoutBffDto(
    long      PayoutRecordId,
    long      TransactionId,
    long      ProviderProfileId,
    string?   ProviderName,
    decimal   GrossVolume,
    decimal   CommissionDeducted,
    decimal   NetPayout,
    string    Status,
    int       ServiceRequestCount,
    string    CurrencyCode,
    string?   GatewayPayoutId,
    DateTime  RequestedAt,
    DateTime? CompletedAt,
    DateTime? HeldAt,
    string?   HoldReason,
    string?   AdminNote
);

/// <summary>Paged payout list.</summary>
public sealed record PaymentPayoutListBffResult(
    List<PaymentPayoutBffDto> Items,
    int                       Total,
    int                       Page,
    int                       PageSize
);

/// <summary>Payout KPI cards for provider-payouts dashboard strip.</summary>
public sealed record PaymentPayoutStatsBffDto(
    decimal PendingTotal,
    int     PendingCount,
    decimal PaidThisMonth,
    int     PaidThisMonthCount,
    decimal OnHoldAmount,
    int     OnHoldCount
);

// ─── Subscription Stats (gap report) ─────────────────────────────────────────

/// <summary>Subscription management KPI strip.</summary>
public sealed record SubscriptionStatsBffDto(
    int     ActiveProviderSubscriptions,
    int     ActiveParticipantSubscriptions,
    decimal MonthlyRecurringRevenue,
    int     FailedRenewalsThisMonth,
    int     ExpiringSoonCount
);
