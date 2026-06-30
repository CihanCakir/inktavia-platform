using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetPaymentTransaction;

public sealed class GetPaymentTransactionQueryHandler
    : AizenQueryHandler<GetPaymentTransactionQuery, PaymentTransactionDto>
{
    private readonly IPaymentTransactionRepository _transactions;

    public GetPaymentTransactionQueryHandler(IPaymentTransactionRepository transactions)
        => _transactions = transactions;

    public override async Task<PaymentTransactionDto?> Handle(
        GetPaymentTransactionQuery request, CancellationToken ct)
    {
        var tx = await _transactions.GetByIdAsync(request.TransactionId, ct);
        if (tx is null) return null;

        return new PaymentTransactionDto(
            tx.Id, tx.TransactionCode, tx.TransactionType, tx.Status,
            tx.PayerProfileId, tx.RecipientProfileId,
            tx.GrossAmount, tx.CommissionAmount, tx.CommissionRateSnapshot,
            tx.VatOnCommission, tx.NetPayoutAmount, tx.DiscountAmount,
            tx.TotalRefundedAmount,
            tx.CurrencyCode, tx.GatewayProvider, tx.GatewayReference,
            tx.EscrowRequired, tx.CapturedAt, tx.ReleasedAt,
            tx.LastRefundedAt, tx.CancelledAt, tx.ReinstatedAt,
            tx.CreateDate);
    }
}
