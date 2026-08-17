using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Application.Queries.GetPayoutList;

public sealed class GetPayoutListQuery : AizenQuery<PayoutListResult>
{
    public PayoutStatus? Status   { get; init; }
    public long?         ProviderId { get; init; }
    public DateTime?     FromDate { get; init; }
    public DateTime?     ToDate   { get; init; }
    public int           Page     { get; init; } = 1;
    public int           PageSize { get; init; } = 25;
}

public sealed record PayoutListItemDto(
    long         PayoutRecordId,
    long         TransactionId,
    long         ProviderProfileId,
    decimal      GrossVolume,          // transaction.GrossAmount
    decimal      CommissionDeducted,   // transaction.CommissionAmount + transaction.VatOnCommission
    decimal      NetPayout,            // payout.Amount (== transaction.NetPayoutAmount)
    int          ServiceRequestCount,  // 1 for SR-context transactions; 0 otherwise
    string       CurrencyCode,
    PayoutStatus Status,
    string?      GatewayPayoutId,
    string?      HoldReason,
    string?      AdminNote,
    DateTime     RequestedAt,
    DateTime?    ProcessedAt,
    DateTime?    HeldAt
);

public sealed record PayoutListResult(
    List<PayoutListItemDto> Items,
    int                     Total,
    int                     Page,
    int                     PageSize
);
