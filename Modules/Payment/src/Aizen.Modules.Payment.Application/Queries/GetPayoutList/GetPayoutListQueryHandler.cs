using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetPayoutList;

[DocumentationInfo("GetPayoutListQueryHandler",
    "Returns a paged list of payout records with optional status/provider/date filters. " +
    "Batch-enriches GrossVolume, CommissionDeducted, NetPayout, and ServiceRequestCount " +
    "from linked PaymentTransactionEntity in a single secondary query.")]
public sealed class GetPayoutListQueryHandler
    : AizenQueryHandler<GetPayoutListQuery, PayoutListResult>
{
    private readonly IPayoutRecordRepository        _payouts;
    private readonly IPaymentTransactionRepository  _transactions;

    public GetPayoutListQueryHandler(
        IPayoutRecordRepository       payouts,
        IPaymentTransactionRepository transactions)
    {
        _payouts      = payouts;
        _transactions = transactions;
    }

    public override async Task<PayoutListResult?> Handle(
        GetPayoutListQuery request, CancellationToken ct)
    {
        var skip = (request.Page - 1) * request.PageSize;

        var (items, total) = await _payouts.GetPagedAsync(
            request.Status,
            request.ProviderId,
            request.FromDate,
            request.ToDate,
            skip, request.PageSize, ct);

        // Batch-fetch linked transactions in a single query.
        var txIds = items
            .Where(p => p.PaymentTransactionId.HasValue)
            .Select(p => p.PaymentTransactionId!.Value)
            .Distinct().ToArray();
        var txMap = txIds.Length > 0
            ? (await _transactions.GetByIdsAsync(txIds, ct)).ToDictionary(t => t.Id)
            : new Dictionary<long, global::Aizen.Modules.Payment.Domain.Entities.Transaction.PaymentTransactionEntity>();

        var dtos = items.Select(p =>
        {
            var tx = p.PaymentTransactionId.HasValue && txMap.TryGetValue(p.PaymentTransactionId.Value, out var t) ? t : null;

            var grossVolume        = tx?.GrossAmount ?? p.Amount;
            var commissionDeducted = tx is not null
                ? tx.CommissionAmount + tx.VatOnCommission
                : 0m;
            var netPayout          = p.Amount;
            var serviceRequestCount = tx?.ContextType == TransactionContextType.ServiceRequest ? 1 : 0;

            return new PayoutListItemDto(
                p.Id,
                p.PaymentTransactionId.GetValueOrDefault(),
                p.ProviderProfileId,
                grossVolume,
                commissionDeducted,
                netPayout,
                serviceRequestCount,
                p.CurrencyCode,
                p.Status,
                p.GatewayPayoutId,
                p.HoldReason,
                p.AdminNote,
                p.RequestedAt,
                p.ProcessedAt,
                p.HeldAt);
        }).ToList();

        return new PayoutListResult(dtos, total, request.Page, request.PageSize);
    }
}
