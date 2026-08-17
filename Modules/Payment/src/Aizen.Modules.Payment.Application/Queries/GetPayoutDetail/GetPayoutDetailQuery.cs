using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Application.Queries.GetPayoutDetail;

public sealed class GetPayoutDetailQuery : AizenQuery<PayoutDetailDto>
{
    public required long PayoutRecordId { get; init; }
}

public sealed record PayoutDetailDto(
    long         PayoutRecordId,
    long         TransactionId,
    long         ProviderProfileId,
    decimal      GrossVolume,          // transaction.GrossAmount
    decimal      CommissionDeducted,   // transaction.CommissionAmount + transaction.VatOnCommission
    decimal      NetPayout,            // payout.Amount
    int          ServiceRequestCount,  // 1 for SR-context transactions; 0 otherwise
    string       CurrencyCode,
    PayoutStatus Status,
    string       GatewayProvider,
    string?      GatewayPayoutId,
    string?      HoldReason,
    string?      FailureReason,
    string?      AdminNote,
    DateTime     RequestedAt,
    DateTime?    ProcessedAt,
    DateTime?    HeldAt
);
