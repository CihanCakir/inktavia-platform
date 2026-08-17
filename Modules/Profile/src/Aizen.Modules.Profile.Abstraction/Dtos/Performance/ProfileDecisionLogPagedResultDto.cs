namespace Aizen.Modules.Profile.Abstraction.Dtos.Performance;

public sealed record ProfileDecisionLogPagedResultDto
{
    public IReadOnlyList<ProfileDecisionLogDto> Items    { get; init; } = [];
    public int                                  Total    { get; init; }
    public int                                  Page     { get; init; }
    public int                                  PageSize { get; init; }
}
