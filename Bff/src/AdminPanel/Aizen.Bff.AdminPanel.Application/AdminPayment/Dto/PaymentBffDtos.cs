using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Dto;

// ─── Transaction ──────────────────────────────────────────────────────────────

/// <summary>
/// BFF-side mirror of PaymentTransactionDto (Application layer).
/// Fields match the JSON serialized by PaymentTransactionController.GetById → return Ok(result).
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
);

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

/// <summary>BFF-side mirror of PendingPayoutDto (Application layer).</summary>
public sealed record PendingPayoutBffDto(
    long     PayoutRecordId,
    long     TransactionId,
    long     ProviderProfileId,
    decimal  Amount,
    string   CurrencyCode,
    string?  GatewayPayoutId,
    DateTime RequestedAt
);

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
