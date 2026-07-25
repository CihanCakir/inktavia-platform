using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Payment.Abstraction.Dto;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetProviderPayoutSummary;

public sealed class GetProviderPayoutSummaryQueryHandler
    : AizenQueryHandler<GetProviderPayoutSummaryQuery, ProviderPayoutSummaryDto>
{
    private readonly IPayoutRecordRepository _payouts;

    public GetProviderPayoutSummaryQueryHandler(IPayoutRecordRepository payouts)
    {
        _payouts = payouts;
    }

    public override async Task<ProviderPayoutSummaryDto?> Handle(
        GetProviderPayoutSummaryQuery request, CancellationToken ct)
    {
        var pid = request.ProviderProfileId;

        var pending    = await _payouts.SumProviderAmountByStatusAsync(pid, [PayoutStatus.Pending, PayoutStatus.Approved], ct);
        var processing = await _payouts.SumProviderAmountByStatusAsync(pid, [PayoutStatus.Processing], ct);
        var completed  = await _payouts.SumProviderAmountByStatusAsync(pid, [PayoutStatus.Completed], ct);
        var onHold     = await _payouts.SumProviderAmountByStatusAsync(pid, [PayoutStatus.OnHold], ct);

        return new ProviderPayoutSummaryDto
        {
            PendingAmount    = pending,
            ProcessingAmount = processing,
            CompletedAmount  = completed,
            OnHoldAmount     = onHold,
            CurrencyCode     = "TRY",
            ComputedAtUtc    = DateTimeOffset.UtcNow,
        };
    }
}
