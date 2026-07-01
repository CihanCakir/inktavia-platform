using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetPayoutDetail;

[DocumentationInfo("GetPayoutDetailQueryHandler",
    "Returns full detail of a single payout record by ID. " +
    "Used by the admin payout detail page.")]
public sealed class GetPayoutDetailQueryHandler
    : AizenQueryHandler<GetPayoutDetailQuery, PayoutDetailDto>
{
    private readonly IPayoutRecordRepository _payouts;

    public GetPayoutDetailQueryHandler(IPayoutRecordRepository payouts)
        => _payouts = payouts;

    public override async Task<PayoutDetailDto?> Handle(
        GetPayoutDetailQuery request, CancellationToken ct)
    {
        var p = await _payouts.GetByIdAsync(request.PayoutRecordId, ct);
        if (p is null) return null;

        return new PayoutDetailDto(
            p.Id, p.PaymentTransactionId, p.ProviderProfileId,
            p.Amount, p.CurrencyCode, p.Status,
            p.GatewayProvider, p.GatewayPayoutId,
            p.HoldReason, p.FailureReason, p.AdminNote,
            p.RequestedAt, p.ProcessedAt, p.HeldAt);
    }
}
