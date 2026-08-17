using Aizen.Bff.AdminPanel.Application.ProfilePerformance.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.ProfilePerformance.Query.GetProfilePerformanceSnapshotsByTier;

public sealed class GetProfilePerformanceSnapshotsByTierBffQuery
    : AizenQuery<GetProfilePerformanceSnapshotsByTierBffResponse>
{
    public string Tier     { get; init; } = default!;
    public int    Page     { get; init; } = 1;
    public int    PageSize { get; init; } = 25;
}

public sealed class GetProfilePerformanceSnapshotsByTierBffResponse
{
    public ProfileSnapshotPagedBffResultDto Data { get; init; } = default!;
}
