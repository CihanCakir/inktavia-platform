using Aizen.Modules.Profile.Abstraction.Enums.Performance;

namespace Aizen.Modules.Profile.Abstraction.Dtos.Performance;

public sealed record ProfileScoreHistoryDto
{
    public long         Id              { get; init; }
    public long         ProfileId       { get; init; }
    public ProfileType  ProfileType     { get; init; }
    public decimal      OverallScore    { get; init; }
    public PriorityTier PriorityTier    { get; init; }
    public string       PriorityTierName => PriorityTier.ToString();
    public decimal      ConfidenceScore { get; init; }
    public int          SampleSize      { get; init; }
    public DateTime     RecordedAtUtc   { get; init; }
    public string?      TriggerReason   { get; init; }
}
