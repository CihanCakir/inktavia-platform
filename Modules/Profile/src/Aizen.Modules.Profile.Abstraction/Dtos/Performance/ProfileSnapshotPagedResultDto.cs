namespace Aizen.Modules.Profile.Abstraction.Dtos.Performance;

public sealed record ProfileSnapshotPagedResultDto
{
    public IReadOnlyList<ProfilePerformanceSnapshotDto> Items    { get; init; } = [];
    public int                                           Total    { get; init; }
    public int                                           Page     { get; init; }
    public int                                           PageSize { get; init; }
}
