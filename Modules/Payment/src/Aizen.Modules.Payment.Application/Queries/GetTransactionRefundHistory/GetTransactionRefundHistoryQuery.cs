using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Application.Queries.GetTransactionRefundHistory;

public sealed class GetTransactionRefundHistoryQuery : AizenQuery<List<TransactionRefundRecordDto>>
{
    public required long TransactionId { get; init; }
}

public sealed record TransactionRefundRecordDto(
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
