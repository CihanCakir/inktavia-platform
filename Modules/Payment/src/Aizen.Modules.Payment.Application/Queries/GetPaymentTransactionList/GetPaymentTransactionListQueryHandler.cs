using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Payment.Application.Queries.GetPaymentTransaction;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetPaymentTransactionList;

public sealed class GetPaymentTransactionListQueryHandler
    : AizenQueryHandler<GetPaymentTransactionListQuery, PaymentTransactionListResult>
{
    private readonly IPaymentTransactionRepository _transactions;

    public GetPaymentTransactionListQueryHandler(IPaymentTransactionRepository transactions)
        => _transactions = transactions;

    public override async Task<PaymentTransactionListResult?> Handle(
        GetPaymentTransactionListQuery request, CancellationToken ct)
    {
        var skip = (request.Page - 1) * request.PageSize;
        var (items, total) = await _transactions.GetPagedAsync(
            request.Status, request.Type, request.Gateway,
            request.FromDate, request.ToDate, request.Search,
            skip, request.PageSize, ct);

        var dtos = items.Select(tx => new PaymentTransactionDto(
            tx.Id, tx.TransactionCode, tx.TransactionType, tx.Status,
            tx.PayerProfileId, tx.RecipientProfileId,
            tx.GrossAmount, tx.CommissionAmount, tx.CommissionRateSnapshot,
            tx.VatOnCommission, tx.NetPayoutAmount, tx.DiscountAmount,
            tx.TotalRefundedAmount,
            tx.CurrencyCode, tx.GatewayProvider, tx.GatewayReference,
            tx.EscrowRequired, tx.CapturedAt, tx.ReleasedAt,
            tx.LastRefundedAt, tx.CancelledAt, tx.ReinstatedAt,
            tx.CreateDate)).ToList();

        return new PaymentTransactionListResult(dtos, total, request.Page, request.PageSize);
    }
}
