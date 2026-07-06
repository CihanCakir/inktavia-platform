namespace Aizen.Modules.Profile.Abstraction.Dtos.Performance;

public sealed record ProfileRiskSignalPagedResultDto
{
    public IReadOnlyList<ProfileRiskSignalDto> Items    { get; init; } = [];
    public int                                 Total    { get; init; }
    public int                                 Page     { get; init; }
    public int                                 PageSize { get; init; }
}
