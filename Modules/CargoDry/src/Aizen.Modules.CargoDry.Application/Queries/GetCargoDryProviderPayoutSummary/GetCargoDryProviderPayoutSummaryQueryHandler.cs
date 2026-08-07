using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryProviderPayoutSummary;

public sealed class GetCargoDryProviderPayoutSummaryQueryHandler
    : AizenQueryHandler<GetCargoDryProviderPayoutSummaryQuery, CargoDryProviderPayoutSummaryDto>
{
    private readonly ICargoDrySellThroughSettlementRepository _settlements;

    public GetCargoDryProviderPayoutSummaryQueryHandler(ICargoDrySellThroughSettlementRepository settlements)
    {
        _settlements = settlements;
    }

    public override async Task<CargoDryProviderPayoutSummaryDto?> Handle(
        GetCargoDryProviderPayoutSummaryQuery request, CancellationToken ct)
    {
        var pid = request.ProviderProfileId;

        var pending   = await _settlements.SumProviderPayoutByStatusAsync(pid, [CargoDrySellThroughSettlementStatus.Pending], ct);
        var ready     = await _settlements.SumProviderPayoutByStatusAsync(pid, [CargoDrySellThroughSettlementStatus.ReadyForSettlement], ct);
        var scheduled = await _settlements.SumProviderPayoutByStatusAsync(pid, [CargoDrySellThroughSettlementStatus.Scheduled], ct);
        var paid      = await _settlements.SumProviderPayoutByStatusAsync(pid, [CargoDrySellThroughSettlementStatus.Settled], ct);
        var disputed  = await _settlements.SumProviderPayoutByStatusAsync(pid, [CargoDrySellThroughSettlementStatus.Disputed], ct);

        return new CargoDryProviderPayoutSummaryDto
        {
            PendingPayout   = pending,
            ReadyPayout     = ready,
            ScheduledPayout = scheduled,
            PaidPayout      = paid,
            DisputedPayout  = disputed,
            CurrencyCode    = "TRY",
            ComputedAtUtc   = DateTimeOffset.UtcNow,
        };
    }
}
