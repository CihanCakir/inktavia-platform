using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetPayoutDetail;

[DocumentationInfo("GetPayoutDetailQueryHandler",
    "Returns full detail of a single payout record by ID. " +
    "Enriches GrossVolume, CommissionDeducted, NetPayout, and ServiceRequestCount " +
    "from the linked PaymentTransactionEntity.")]
public sealed class GetPayoutDetailQueryHandler
    : AizenQueryHandler<GetPayoutDetailQuery, PayoutDetailDto>
{
    private readonly IPayoutRecordRepository        _payouts;
    private readonly IPaymentTransactionRepository  _transactions;

    public GetPayoutDetailQueryHandler(
        IPayoutRecordRepository       payouts,
        IPaymentTransactionRepository transactions)
    {
        _payouts      = payouts;
        _transactions = transactions;
    }

    public override async Task<PayoutDetailDto?> Handle(
        GetPayoutDetailQuery request, CancellationToken ct)
    {
        var p = await _payouts.GetByIdAsync(request.PayoutRecordId, ct);
        if (p is null) return null;

        var tx = p.PaymentTransactionId.HasValue
            ? await _transactions.GetByIdAsync(p.PaymentTransactionId.Value, ct)
            : null;

        var grossVolume        = tx?.GrossAmount ?? p.Amount;
        var commissionDeducted = tx is not null
            ? tx.CommissionAmount + tx.VatOnCommission
            : 0m;
        var serviceRequestCount = tx?.ContextType == TransactionContextType.ServiceRequest ? 1 : 0;

        return new PayoutDetailDto(
            p.Id,
            p.PaymentTransactionId.GetValueOrDefault(),
            p.ProviderProfileId,
            grossVolume,
            commissionDeducted,
            p.Amount,       // NetPayout
            serviceRequestCount,
            p.CurrencyCode,
            p.Status,
            p.GatewayProvider,
            p.GatewayPayoutId,
            p.HoldReason,
            p.FailureReason,
            p.AdminNote,
            p.RequestedAt,
            p.ProcessedAt,
            p.HeldAt);
    }
}
