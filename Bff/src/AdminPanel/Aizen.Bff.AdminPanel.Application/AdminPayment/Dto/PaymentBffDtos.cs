using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Dto;

// ─── Transaction ──────────────────────────────────────────────────────────────

/// <summary>
/// BFF-side mirror of PaymentTransactionDto (Application layer).
/// Fields match the JSON serialized by PaymentTransactionController.GetById → return Ok(result).
/// BFF enriches identity display names and optionally SR context from cross-module calls.
/// </summary>
public sealed record PaymentTransactionBffDto(
    long                     TransactionId,
    string                   TransactionCode,
    string                   TransactionType,   // string, not enum — BFF JSON serializer lacks StringEnumConverter
    string                   Status,            // string, not enum — BFF JSON serializer lacks StringEnumConverter
    // ── Transaction context (what triggered this payment) ────────────────────
    string                   ContextType,       // string, not enum — BFF JSON serializer lacks StringEnumConverter
    long                     ContextId,
    long?                    ContextSubId,
    // ── Parties ───────────────────────────────────────────────────────────────
    long                     PayerProfileId,
    long?                    RecipientProfileId,
    // ── Amounts ───────────────────────────────────────────────────────────────
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
    // ── Identity enrichment (BFF resolves from Identity module) ───────────────
    /// <summary>Display name of the payer — resolved from Identity module by BFF.</summary>
    public string? PayerDisplayName     { get; init; }
    /// <summary>Display name of the recipient — resolved from Identity module by BFF.</summary>
    public string? RecipientDisplayName { get; init; }

    // ── ServiceRequest enrichment (BFF resolves when ContextType == ServiceRequest) ──
    /// <summary>ServiceRequest code (e.g. "SR-20241215-0042") — null when ContextType ≠ ServiceRequest.</summary>
    public string?              ServiceRequestCode   { get; init; }
    /// <summary>ServiceRequest title — null when ContextType ≠ ServiceRequest.</summary>
    public string?              ServiceRequestTitle  { get; init; }
    /// <summary>ServiceRequest status — null when ContextType ≠ ServiceRequest.</summary>
    public ServiceRequestStatus? ServiceRequestStatus { get; init; }
    /// <summary>VesselId linked to the ServiceRequest — null when ContextType ≠ ServiceRequest.</summary>
    public long?                ServiceRequestVesselId { get; init; }
    /// <summary>ServiceRequest Id — null when ContextType ≠ ServiceRequest.</summary>
    public long?                ServiceRequestId { get; init; }
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
    string                  Status,   // string, not enum — BFF JSON serializer lacks StringEnumConverter
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

/// <summary>BFF-side mirror of PlanFeatureItem value object.</summary>
public sealed record PlanFeatureItemBffDto(string Text, bool IsHighlighted = false);

/// <summary>BFF-side mirror of ProviderPlanDto (Application layer).</summary>
public sealed record ProviderPlanBffDto(
    long    Id,
    string  PlanCode,
    string  Name,
    string? Description,
    decimal MonthlyPriceTRY,
    decimal? AnnualPriceTRY,
    int?    TrialDays,
    string? BadgeLabel,
    int?    MaxActiveOffers,
    bool    HasPriorityBoost,
    bool    HasFullAnalytics,
    bool    IsFree,
    bool    IsActive,
    int     SortOrder,
    DateTime? ValidFrom,
    DateTime? ValidTo,
    List<PlanFeatureItemBffDto> FeatureItems
);

/// <summary>BFF-side mirror of ParticipantPlanDto (Application layer).</summary>
public sealed record ParticipantPlanBffDto(
    long    Id,
    string  PlanCode,
    string  Name,
    string? Description,
    decimal MonthlyPriceTRY,
    decimal? AnnualPriceTRY,
    int?    TrialDays,
    string? BadgeLabel,
    decimal ServiceDiscountRate,
    decimal CargoDryDiscountRate,
    decimal InkCoinEarnMultiplier,
    bool    IsFree,
    bool    IsActive,
    int     SortOrder,
    DateTime? ValidFrom,
    DateTime? ValidTo,
    List<PlanFeatureItemBffDto> FeatureItems
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

// ─── Plan CRUD (request/result DTOs) ──────────────────────────────────────────

/// <summary>Shared result for create/update plan operations.</summary>
public sealed record PlanMutateBffResult(long Id, string PlanCode);

/// <summary>Request body for creating a provider plan.</summary>
public sealed record CreateProviderPlanBffRequest(
    string  PlanCode,
    string  Name,
    string? Description,
    decimal MonthlyPriceTRY,
    decimal? AnnualPriceTRY,
    int?    TrialDays,
    string? BadgeLabel,
    int?    MaxActiveOffers,
    bool    HasPriorityBoost,
    bool    HasFullAnalytics,
    int     SortOrder,
    DateTime? ValidFrom,
    DateTime? ValidTo,
    List<PlanFeatureItemBffDto> FeatureItems
);

/// <summary>Request body for updating a provider plan (PlanCode immutable).</summary>
public sealed record UpdateProviderPlanBffRequest(
    string  Name,
    string? Description,
    decimal MonthlyPriceTRY,
    decimal? AnnualPriceTRY,
    int?    TrialDays,
    string? BadgeLabel,
    int?    MaxActiveOffers,
    bool    HasPriorityBoost,
    bool    HasFullAnalytics,
    int     SortOrder,
    DateTime? ValidFrom,
    DateTime? ValidTo,
    List<PlanFeatureItemBffDto> FeatureItems
);

/// <summary>Request body for creating a participant plan.</summary>
public sealed record CreateParticipantPlanBffRequest(
    string  PlanCode,
    string  Name,
    string? Description,
    decimal MonthlyPriceTRY,
    decimal? AnnualPriceTRY,
    int?    TrialDays,
    string? BadgeLabel,
    decimal ServiceDiscountRate,
    decimal CargoDryDiscountRate,
    decimal InkCoinEarnMultiplier,
    int     SortOrder,
    DateTime? ValidFrom,
    DateTime? ValidTo,
    List<PlanFeatureItemBffDto> FeatureItems
);

/// <summary>Request body for updating a participant plan (PlanCode immutable).</summary>
public sealed record UpdateParticipantPlanBffRequest(
    string  Name,
    string? Description,
    decimal MonthlyPriceTRY,
    decimal? AnnualPriceTRY,
    int?    TrialDays,
    string? BadgeLabel,
    decimal ServiceDiscountRate,
    decimal CargoDryDiscountRate,
    decimal InkCoinEarnMultiplier,
    int     SortOrder,
    DateTime? ValidFrom,
    DateTime? ValidTo,
    List<PlanFeatureItemBffDto> FeatureItems
);

// ─── Admin Subscription List ──────────────────────────────────────────────────

/// <summary>Single row in the admin subscription oversight list (provider or participant).</summary>
public sealed record AdminSubscriptionListItemBffDto(
    long     Id,
    string   Audience,            // "Provider" | "Participant"
    long     ProfileId,
    long     UserId,              // Identity userId — used for navigation to user detail page
    string   ProfileDisplayName,  // "FirstName LastName" resolved from Identity
    long     PlanId,
    string   PlanCode,
    string   PlanName,
    string   Status,              // "Active" | "PastDue" | "Cancelled" | "Expired"
    decimal  PaidAmount,
    string   CurrencyCode,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    bool     AutoRenew,
    DateTime CreateDate
);

/// <summary>Paged result for the admin subscription list endpoint.</summary>
public sealed record AdminSubscriptionListBffResult(
    List<AdminSubscriptionListItemBffDto> Items,
    int Total,
    int Page,
    int PageSize
);

// ─── MRR Trend ────────────────────────────────────────────────────────────────

public sealed record MrrMonthBffDto(
    int     Year,
    int     Month,
    string  Label,
    decimal MrrTotal
);

public sealed record SubscriptionMrrTrendBffResult(
    List<MrrMonthBffDto> Months
);

// ─── Churn Risk ───────────────────────────────────────────────────────────────

public sealed record SubscriptionChurnRiskBffDto(
    int PaymentFailureRiskCount,
    int ExpiringIn7DaysCount,
    int TotalAtRiskCount
);
