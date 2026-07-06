using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Profile.Abstraction.Dtos.Performance;
using Aizen.Modules.Profile.Application.Queries.Performance.GetPerformanceSnapshotByProfile;
using Aizen.Modules.Profile.Domain.Interface.Repository;

namespace Aizen.Modules.Profile.Application.Queries.Performance.GetPerformanceSnapshotsByTier;

[DocumentationInfo("GetPerformanceSnapshotsByTierQueryHandler",
    "Returns a paged list of performance snapshots filtered by PriorityTier. " +
    "Used by admin dashboards to view all Platinum, Gold, Silver, Standard, or Flagged providers.")]
public sealed class GetPerformanceSnapshotsByTierQueryHandler
    : AizenQueryHandler<GetPerformanceSnapshotsByTierQuery, ProfileSnapshotPagedResultDto>
{
    private readonly IProfilePerformanceSnapshotRepository _snapshots;

    public GetPerformanceSnapshotsByTierQueryHandler(
        IProfilePerformanceSnapshotRepository snapshots)
    {
        _snapshots = snapshots;
    }

    public override async Task<ProfileSnapshotPagedResultDto> Handle(
        GetPerformanceSnapshotsByTierQuery request, CancellationToken ct)
    {
        var skip  = (request.Page - 1) * request.PageSize;
        var total = await _snapshots.CountByTierAsync(request.Tier, ct);
        var items = await _snapshots.GetByTierAsync(request.Tier, skip, request.PageSize, ct);

        return new ProfileSnapshotPagedResultDto
        {
            Items    = items.Select(GetPerformanceSnapshotByProfileQueryHandler.MapSnapshot).ToList(),
            Total    = total,
            Page     = request.Page,
            PageSize = request.PageSize,
        };
    }
}
