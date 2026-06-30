using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Application.Queries.GetPaymentTransaction;

public sealed class GetPaymentTransactionQuery : AizenQuery<PaymentTransactionDto>
{
    public required long TransactionId { get; init; }
}

public sealed record PaymentTransactionDto(
    long TransactionId,
    string TransactionCode,
    TransactionType TransactionType,
    PaymentTransactionStatus Status,
    long PayerProfileId,
    long? RecipientProfileId,
    decimal GrossAmount,
    decimal CommissionAmount,
    decimal CommissionRateSnapshot,
    decimal VatOnCommission,
    decimal NetPayoutAmount,
    decimal DiscountAmount,
    decimal TotalRefundedAmount,
    string CurrencyCode,
    string GatewayProvider,
    string? GatewayReference,
    bool EscrowRequired,
    DateTime? CapturedAt,
    DateTime? ReleasedAt,
    DateTime? LastRefundedAt,
    DateTime? CancelledAt,
    DateTime? ReinstatedAt,
    DateTime? CreateDate
);
