using Aizen.Modules.Profile.Abstraction.Enums.Performance;

namespace Aizen.Modules.Profile.Abstraction.Dtos.Performance;

public sealed record ProfileRiskSignalDto
{
    public long               Id               { get; init; }
    public long               ProfileId        { get; init; }
    public ProfileType        ProfileType      { get; init; }
    public RiskSignalSeverity Severity         { get; init; }
    public string             SeverityName     => Severity.ToString();
    public string             SignalCode       { get; init; } = default!;
    public string             Description      { get; init; } = default!;
    public string?            SourceModule     { get; init; }
    public long?              SourceEntityId   { get; init; }
    public DateTime           DetectedAtUtc    { get; init; }
    public bool               IsResolved       { get; init; }
    public DateTime?          ResolvedAtUtc    { get; init; }
    public string?            ResolutionNote   { get; init; }
    public long?              ResolvedByUserId { get; init; }
}
