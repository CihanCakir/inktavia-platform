using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetPendingPayouts;

public sealed class GetPendingPayoutsQueryHandler
    : AizenQueryHandler<GetPendingPayoutsQuery, List<PendingPayoutDto>>
{
    private readonly IPayoutRecordRepository _payouts;

    public GetPendingPayoutsQueryHandler(IPayoutRecordRepository payouts)
        => _payouts = payouts;

    public override async Task<List<PendingPayoutDto>?> Handle(
        GetPendingPayoutsQuery request, CancellationToken ct)
    {
        var pending = await _payouts.GetPendingAsync(ct);
        return pending.Select(p => new PendingPayoutDto(
            p.Id, p.PaymentTransactionId, p.ProviderProfileId,
            p.Amount, p.CurrencyCode, p.GatewayPayoutId, p.RequestedAt)).ToList();
    }
}
