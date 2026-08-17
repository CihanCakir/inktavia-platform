using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetTransactionRefundHistory;

public sealed class GetTransactionRefundHistoryQueryHandler
    : AizenQueryHandler<GetTransactionRefundHistoryQuery, List<TransactionRefundRecordDto>>
{
    private readonly IPaymentTransactionRepository _transactions;

    public GetTransactionRefundHistoryQueryHandler(IPaymentTransactionRepository transactions)
        => _transactions = transactions;

    public override async Task<List<TransactionRefundRecordDto>?> Handle(
        GetTransactionRefundHistoryQuery request, CancellationToken ct)
    {
        var records = await _transactions.GetRefundRecordsAsync(request.TransactionId, ct);

        return records.Select(r => new TransactionRefundRecordDto(
            r.Id,
            r.RefundCode,
            r.RefundType,
            r.Reason,
            r.Status,
            r.Amount,
            r.CurrencyCode,
            r.GatewayRefundReference,
            r.ProcessedAt,
            r.FailureReason,
            r.AdminNote,
            r.ReversedAt,
            r.ReversalReason,
            r.ReversalAdminNote,
            r.CreateDate
        )).ToList();
    }
}
