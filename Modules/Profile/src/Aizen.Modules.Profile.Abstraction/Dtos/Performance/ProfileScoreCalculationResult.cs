using Aizen.Modules.Profile.Abstraction.Enums.Performance;

namespace Aizen.Modules.Profile.Abstraction.Dtos.Performance;

/// <summary>
/// Output record produced by ProfilePerformanceEngine.CalculateAsync().
/// Contains all computed scores, confidence, tier classification, and component breakdowns.
/// This is NOT persisted directly — it drives the Upsert command.
/// </summary>
public sealed record ProfileScoreCalculationResult
{
    // ── Identity ──────────────────────────────────────────────────────────────
    public long        ProfileId   { get; init; }
    public ProfileType ProfileType { get; init; }

    // ── Dimension Scores (0–100 each) ─────────────────────────────────────────
    public decimal ServiceRequestScore        { get; init; }
    public decimal CargoDryScore              { get; init; }
    public decimal OperationalDisciplineScore { get; init; }
    public decimal FinancialReliabilityScore  { get; init; }
    public decimal PlatformComplianceScore    { get; init; }
    public decimal RiskPenaltyScore           { get; init; }

    // ── Overall (clamped 0–100) ────────────────────────────────────────────────
    public decimal OverallScore { get; init; }

    // ── Confidence ────────────────────────────────────────────────────────────
    public decimal                    ConfidenceScore { get; init; }
    public PerformanceConfidenceLevel ConfidenceLevel { get; init; }
    public int                        SampleSize      { get; init; }
    public bool                       IsColdStart     { get; init; }

    // ── Tier ──────────────────────────────────────────────────────────────────
    public PriorityTier DerivedTier { get; init; }

    // ── Component Detail ──────────────────────────────────────────────────────
    /// <summary>One entry per PerformanceScoreCategory for storage as score_components rows.</summary>
    public IReadOnlyList<ProfileScoreComponentDto> Components { get; init; }
        = Array.Empty<ProfileScoreComponentDto>();

    // ── Metadata ──────────────────────────────────────────────────────────────
    /// <summary>JSON metadata bag stored on the snapshot (e.g. {"coldStart":true,"calculationVersion":"v1"}).</summary>
    public string? MetadataJson { get; init; }

    public DateTime CalculatedAtUtc { get; init; } = DateTime.UtcNow;
}
