using Aizen.Bff.AdminPanel.Application.AdminProfilePerformance.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminProfilePerformance.Query.GetProfilePerformanceSnapshotsByTier;

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
