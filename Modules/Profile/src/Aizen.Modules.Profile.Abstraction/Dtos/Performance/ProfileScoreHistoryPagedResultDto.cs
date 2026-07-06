namespace Aizen.Modules.Profile.Abstraction.Dtos.Performance;

public sealed record ProfileScoreHistoryPagedResultDto
{
    public IReadOnlyList<ProfileScoreHistoryDto> Items    { get; init; } = [];
    public int                                   Total    { get; init; }
    public int                                   Page     { get; init; }
    public int                                   PageSize { get; init; }
}
