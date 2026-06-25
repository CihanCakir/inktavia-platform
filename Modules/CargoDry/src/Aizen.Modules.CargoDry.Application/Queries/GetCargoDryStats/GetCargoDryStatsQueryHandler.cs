using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryStats;

public sealed class GetCargoDryStatsQueryHandler
    : AizenQueryHandler<GetCargoDryStatsQuery, CargoDryStatsDto>
{
    private readonly ICargoDryKitRepository   _kits;
    private readonly ICargoDryBatchRepository _batches;

    public GetCargoDryStatsQueryHandler(ICargoDryKitRepository kits, ICargoDryBatchRepository batches)
    {
        _kits    = kits;
        _batches = batches;
    }

    public override async Task<CargoDryStatsDto> Handle(GetCargoDryStatsQuery request, CancellationToken ct)
    {
        var stats   = await _kits.GetStatsAsync(ct);
        var batches = await _batches.GetAllAsync(ct);
        var activeBatchCount = batches.Count(b => !b.IsRevoked);

        return new CargoDryStatsDto
        {
            TotalKits          = stats.Total,
            AvailableKits      = stats.Available,
            ActiveKits         = stats.Active,
            ExpiringKits       = stats.Expiring,
            ExpiredKits        = stats.Expired,
            RevokedKits        = stats.Revoked,
            TodayActivations   = stats.TodayActivations,
            TotalBatches       = activeBatchCount,
            RenewalRatePercent = 0,
        };
    }
}
