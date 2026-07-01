using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Application.Queries.GetPaymentTransaction;

public sealed class GetPaymentTransactionQuery : AizenQuery<PaymentTransactionDto>
{
    public required long TransactionId { get; init; }
}

public sealed record PaymentTransactionDto(
    long                     TransactionId,
    string                   TransactionCode,
    TransactionType          TransactionType,
    PaymentTransactionStatus Status,
    // ── Context: what entity triggered this transaction ──────────────────────
    TransactionContextType   ContextType,
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
);
