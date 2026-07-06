using Aizen.Modules.Profile.Abstraction.Enums.Performance;

namespace Aizen.Modules.Profile.Abstraction.Dtos.Performance;

public sealed record ProfilePerformanceSnapshotDto
{
    public long        ProfileId   { get; init; }
    public ProfileType ProfileType { get; init; }
    public string      ProfileTypeName => ProfileType.ToString();

    public PriorityTier PriorityTier     { get; init; }
    public string       PriorityTierName => PriorityTier.ToString();

    public decimal OverallScore               { get; init; }
    public decimal ServiceRequestScore        { get; init; }
    public decimal CargoDryScore              { get; init; }
    public decimal OperationalDisciplineScore { get; init; }
    public decimal FinancialReliabilityScore  { get; init; }
    public decimal PlatformComplianceScore    { get; init; }
    public decimal RiskPenaltyScore           { get; init; }

    public decimal                    ConfidenceScore { get; init; }
    public PerformanceConfidenceLevel ConfidenceLevel { get; init; }
    public int                        SampleSize      { get; init; }
    public bool                       IsColdStart     { get; init; }

    public bool                HasActiveRiskSignal        { get; init; }
    public RiskSignalSeverity? ActiveRiskSignalMaxSeverity { get; init; }

    public DateTime? LastCalculatedAtUtc { get; init; }
    public DateTime? ValidFromUtc        { get; init; }
    public string?   MetadataJson        { get; init; }
}
