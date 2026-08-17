using Aizen.Core.Domain;
using Aizen.Modules.Profile.Abstraction.Enums.Performance;

namespace Aizen.Modules.Profile.Domain.Entities.Performance;

/// <summary>
/// Current-state performance snapshot for a single profile (Provider, Owner, or Participant).
/// One row per (ProfileId, ProfileType) combination — upserted on every score recalculation.
///
/// Score formula (Phase 19 spec):
///   OverallScore =
///     ServiceRequestScore        * 0.35
///   + CargoDryScore              * 0.25
///   + OperationalDisciplineScore * 0.15
///   + FinancialReliabilityScore  * 0.15
///   + PlatformComplianceScore    * 0.10
///   - RiskPenaltyScore
///   (clamped 0–100)
///
/// Cold-start rule: SampleSize &lt; 5 → OverallScore = 50, PriorityTier = Standard,
/// ConfidenceScore ≤ 0.25, MetadataJson includes "coldStart": true.
/// </summary>
[DocumentationInfo("ProfilePerformanceSnapshotEntity",
    "Current-state performance snapshot per (ProfileId, ProfileType). " +
    "Upserted on every score recalculation. Contains overall + per-dimension scores, " +
    "priority tier, confidence score, and active risk signal flag.")]
public sealed class ProfilePerformanceSnapshotEntity : AizenEntityWithAudit
{
    // ── Identity ──────────────────────────────────────────────────────────────
    /// <summary>Cross-schema FK to profile.user_profiles.Id (not enforced at DB level).</summary>
    public long        ProfileId   { get; private set; }
    public ProfileType ProfileType { get; private set; }

    // ── Tier ──────────────────────────────────────────────────────────────────
    public PriorityTier PriorityTier { get; private set; }

    // ── Scores ────────────────────────────────────────────────────────────────
    public decimal OverallScore               { get; private set; }  // 0–100, clamped
    public decimal ServiceRequestScore        { get; private set; }  // 0–100
    public decimal CargoDryScore              { get; private set; }  // 0–100
    public decimal OperationalDisciplineScore { get; private set; }  // 0–100
    public decimal FinancialReliabilityScore  { get; private set; }  // 0–100
    public decimal PlatformComplianceScore    { get; private set; }  // 0–100
    public decimal RiskPenaltyScore           { get; private set; }  // deducted; 0 if no risk

    // ── Confidence ────────────────────────────────────────────────────────────
    /// <summary>0.0–1.0. Formula: min(1.0, SampleSize / 20.0).</summary>
    public decimal                    ConfidenceScore { get; private set; }
    public PerformanceConfidenceLevel ConfidenceLevel { get; private set; }
    /// <summary>Number of completed service requests / kit activations used in calculation.</summary>
    public int                        SampleSize      { get; private set; }

    // ── Risk ──────────────────────────────────────────────────────────────────
    public bool               HasActiveRiskSignal        { get; private set; }
    public RiskSignalSeverity? ActiveRiskSignalMaxSeverity { get; private set; }

    // ── Timestamps ────────────────────────────────────────────────────────────
    public DateTime? LastCalculatedAtUtc { get; private set; }
    public DateTime? ValidFromUtc        { get; private set; }

    // ── Metadata ──────────────────────────────────────────────────────────────
    /// <summary>JSON bag for extended data (e.g. {"coldStart":true,"calculationVersion":"v1"}).</summary>
    public string? MetadataJson { get; private set; }

    private ProfilePerformanceSnapshotEntity() { }

    public static ProfilePerformanceSnapshotEntity Create(
        long                      profileId,
        ProfileType               profileType,
        decimal                   overallScore,
        decimal                   serviceRequestScore,
        decimal                   cargoDryScore,
        decimal                   operationalDisciplineScore,
        decimal                   financialReliabilityScore,
        decimal                   platformComplianceScore,
        decimal                   riskPenaltyScore,
        decimal                   confidenceScore,
        PerformanceConfidenceLevel confidenceLevel,
        int                       sampleSize,
        PriorityTier              priorityTier,
        string?                   metadataJson = null)
    {
        return new ProfilePerformanceSnapshotEntity
        {
            ProfileId                  = profileId,
            ProfileType                = profileType,
            OverallScore               = Math.Clamp(overallScore, 0m, 100m),
            ServiceRequestScore        = Math.Clamp(serviceRequestScore, 0m, 100m),
            CargoDryScore              = Math.Clamp(cargoDryScore, 0m, 100m),
            OperationalDisciplineScore = Math.Clamp(operationalDisciplineScore, 0m, 100m),
            FinancialReliabilityScore  = Math.Clamp(financialReliabilityScore, 0m, 100m),
            PlatformComplianceScore    = Math.Clamp(platformComplianceScore, 0m, 100m),
            RiskPenaltyScore           = Math.Max(0m, riskPenaltyScore),
            ConfidenceScore            = Math.Clamp(confidenceScore, 0m, 1m),
            ConfidenceLevel            = confidenceLevel,
            SampleSize                 = Math.Max(0, sampleSize),
            PriorityTier               = priorityTier,
            HasActiveRiskSignal        = false,
            MetadataJson               = metadataJson,
            LastCalculatedAtUtc        = DateTime.UtcNow,
            ValidFromUtc               = DateTime.UtcNow,
            IsActive                   = true,
        };
    }

    /// <summary>Recalculates all score fields in place (mutable current-state update).</summary>
    public void UpdateScore(
        decimal                   overallScore,
        decimal                   serviceRequestScore,
        decimal                   cargoDryScore,
        decimal                   operationalDisciplineScore,
        decimal                   financialReliabilityScore,
        decimal                   platformComplianceScore,
        decimal                   riskPenaltyScore,
        decimal                   confidenceScore,
        PerformanceConfidenceLevel confidenceLevel,
        int                       sampleSize,
        PriorityTier              priorityTier,
        string?                   metadataJson = null)
    {
        OverallScore               = Math.Clamp(overallScore, 0m, 100m);
        ServiceRequestScore        = Math.Clamp(serviceRequestScore, 0m, 100m);
        CargoDryScore              = Math.Clamp(cargoDryScore, 0m, 100m);
        OperationalDisciplineScore = Math.Clamp(operationalDisciplineScore, 0m, 100m);
        FinancialReliabilityScore  = Math.Clamp(financialReliabilityScore, 0m, 100m);
        PlatformComplianceScore    = Math.Clamp(platformComplianceScore, 0m, 100m);
        RiskPenaltyScore           = Math.Max(0m, riskPenaltyScore);
        ConfidenceScore            = Math.Clamp(confidenceScore, 0m, 1m);
        ConfidenceLevel            = confidenceLevel;
        SampleSize                 = Math.Max(0, sampleSize);
        PriorityTier               = priorityTier;
        MetadataJson               = metadataJson;
        LastCalculatedAtUtc        = DateTime.UtcNow;
    }

    /// <summary>Sets the active risk signal flag. Tier should be updated to Flagged separately.</summary>
    public void FlagRiskSignal(RiskSignalSeverity maxSeverity)
    {
        HasActiveRiskSignal         = true;
        ActiveRiskSignalMaxSeverity = maxSeverity;
        PriorityTier                = PriorityTier.Flagged;
    }

    /// <summary>Clears the risk signal flag and recalculates tier from current score.</summary>
    public void ClearRiskSignalFlag(PriorityTier recalculatedTier)
    {
        HasActiveRiskSignal         = false;
        ActiveRiskSignalMaxSeverity = null;
        PriorityTier                = recalculatedTier;
    }
}
