using Aizen.Modules.Profile.Abstraction.Enums.Performance;

namespace Aizen.Modules.Profile.Abstraction.Dtos.Performance;

public sealed record ProfileScoreComponentDto
{
    public long                     SnapshotId           { get; init; }
    public PerformanceScoreCategory Category             { get; init; }
    public string                   CategoryName         => Category.ToString();
    public decimal                  RawScore             { get; init; }
    public decimal                  Weight               { get; init; }
    public decimal                  WeightedContribution { get; init; }
    public int                      MetricCount          { get; init; }
    public string?                  MetricsJson          { get; init; }
    public DateTime                 CalculatedAtUtc      { get; init; }
}
