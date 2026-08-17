using Aizen.Bff.AdminPanel.Application.ProfilePerformance.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.ProfilePerformance.Query.GetProfilePerformanceSnapshot;

public sealed class GetProfilePerformanceSnapshotBffQuery
    : AizenQuery<GetProfilePerformanceSnapshotBffResponse>
{
    public long   ProfileId   { get; init; }
    public string ProfileType { get; init; } = "Provider";
}

public sealed class GetProfilePerformanceSnapshotBffResponse
{
    public ProfilePerformanceSnapshotWithComponentsBffDto Data { get; init; } = default!;
}
