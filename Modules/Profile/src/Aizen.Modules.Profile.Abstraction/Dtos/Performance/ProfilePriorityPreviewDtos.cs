using Aizen.Modules.Profile.Abstraction.Enums.Performance;

namespace Aizen.Modules.Profile.Abstraction.Dtos.Performance;

// ─── Request ──────────────────────────────────────────────────────────────────

/// <summary>
/// Input for a priority preview query.
/// Phase 21 — read-only admin decision support only.
/// Hard rule: this does NOT change provider assignment, ranking, or scoring.
/// Supported contexts (MVP): ServiceRequestProviderRecommendation, AdminAssignmentSuggestion.
/// Deferred: CargoDryOpportunityRouting, ProviderSearchRanking.
/// </summary>
public sealed class ProfilePriorityPreviewRequestDto
{
    /// <summary>Provider profile IDs to rank. 1–50 entries.</summary>
    public List<long> CandidateProfileIds { get; init; } = [];

    /// <summary>
    /// Usage context. Supported: ServiceRequestProviderRecommendation | AdminAssignmentSuggestion.
    /// </summary>
    public string Context { get; init; } = default!;

    /// <summary>Optional service category code (used in future for CategoryFit — MVP: neutral 50).</summary>
    public string? CategoryCode { get; init; }

    /// <summary>Optional location code (used in future for LocationFit — MVP: neutral 50).</summary>
    public string? LocationCode { get; init; }

    /// <summary>Maximum number of ranked candidates to return. Default 10, max 50.</summary>
    public int MaxResults { get; init; } = 10;

    /// <summary>
    /// If true, appends a PriorityPreviewGenerated entry to the decision log for audit purposes.
    /// Phase 21 rule: logs are append-only — never mutates existing records.
    /// </summary>
    public bool LogDecision { get; init; } = false;

    /// <summary>Keycloak sub claim of the admin triggering the preview (for audit log).</summary>
    public string? ActorUserId { get; init; }
}

// ─── Response ─────────────────────────────────────────────────────────────────

/// <summary>
/// Priority preview result — ranked list of candidates with explanation factors.
/// Phase 21 rule: read-only. No assignment changes. No scoring changes.
/// </summary>
public sealed class ProfilePriorityPreviewResultDto
{
    public List<ProfilePriorityCandidateDto> Items           { get; init; } = [];
    public string                            Context         { get; init; } = default!;
    public DateTime                          GeneratedAtUtc  { get; init; } = DateTime.UtcNow;
    public string                            ExplanationSummary { get; init; } = default!;
    public int                               RequestedCount  { get; init; }
    public int                               ResolvedCount   { get; init; }
    public int                               SkippedCount    { get; init; }
}

/// <summary>A single ranked candidate with priority score and explanation.</summary>
public sealed class ProfilePriorityCandidateDto
{
    public long        ProfileId       { get; init; }
    public ProfileType ProfileType     { get; init; }
    public decimal     OverallScore    { get; init; }
    public PriorityTier PriorityTier   { get; init; }
    public decimal     PriorityScore   { get; init; }
    public int         Rank            { get; set; }  // set after sort — not init-only
    public bool        IsColdStart     { get; init; }
    public PerformanceConfidenceLevel ConfidenceLevel { get; init; }
    public int         SampleSize      { get; init; }
    public bool        HasActiveRiskSignal { get; init; }
    public string?     ActiveRiskSignalMaxSeverity { get; init; }

    public List<ProfilePriorityExplanationFactorDto> ExplanationFactors { get; init; } = [];
}

/// <summary>A single explanation factor contributing to the priority score.</summary>
public sealed class ProfilePriorityExplanationFactorDto
{
    /// <summary>Human-readable factor name (e.g. "OverallScore", "CategoryFit", "RiskPenalty").</summary>
    public string  Factor          { get; init; } = default!;

    /// <summary>The raw input value for this factor (0–100 or penalty value).</summary>
    public decimal Value           { get; init; }

    /// <summary>Weight applied to this factor in the priority formula.</summary>
    public decimal Weight          { get; init; }

    /// <summary>Weighted contribution to the final PriorityScore (Value * Weight, sign-aware).</summary>
    public decimal Contribution    { get; init; }

    /// <summary>True when this factor is using a neutral MVP placeholder (not real data).</summary>
    public bool    IsMvpNeutral    { get; init; }

    /// <summary>Optional note explaining the factor or MVP status.</summary>
    public string? Note            { get; init; }
}
