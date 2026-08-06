using System.Text.Json.Serialization;

namespace Aizen.Bff.AdminPanel.Application.ProfilePerformance.Dto;

// ── Snapshot ──────────────────────────────────────────────────────────────────

public sealed class ProfilePerformanceSnapshotBffDto
{
    public long    ProfileId                  { get; init; }
    public string  ProfileType                { get; init; } = default!;
    public decimal OverallScore               { get; init; }
    public string  PriorityTier               { get; init; } = default!;
    public decimal ConfidenceScore            { get; init; }
    public string  ConfidenceLevel            { get; init; } = default!;
    public int     SampleSize                 { get; init; }
    public decimal ServiceRequestScore        { get; init; }
    public decimal CargoDryScore              { get; init; }
    public decimal OperationalDisciplineScore { get; init; }
    public decimal FinancialReliabilityScore  { get; init; }
    public decimal PlatformComplianceScore    { get; init; }
    public decimal RiskPenaltyScore           { get; init; }
    public bool    HasActiveRiskSignal        { get; init; }
    public string? ActiveRiskSignalMaxSeverity { get; init; }
    // Phase 20H: backend field is LastCalculatedAtUtc (nullable). JsonPropertyName maps the upstream
    // Profile module JSON to this BFF field. The BFF outputs this as computedAtUtc to the Admin Web.
    [JsonPropertyName("lastCalculatedAtUtc")]
    public DateTime? ComputedAtUtc            { get; init; }
    // Phase 20H: added — mirrors backend ProfilePerformanceSnapshotDto.IsColdStart
    public bool     IsColdStart               { get; init; }
    // Phase 20H: added — mirrors backend ProfilePerformanceSnapshotDto.ValidFromUtc
    public DateTime? ValidFromUtc             { get; init; }
    public string?  MetadataJson              { get; init; }
}

public sealed class ProfilePerformanceSnapshotWithComponentsBffDto
{
    public bool                                  Found      { get; init; }
    public ProfilePerformanceSnapshotBffDto?     Snapshot   { get; init; }
    public List<ProfileScoreComponentBffDto>     Components { get; init; } = [];
}

// ── Score Components ──────────────────────────────────────────────────────────

public sealed class ProfileScoreComponentBffDto
{
    public long    SnapshotId            { get; init; }
    public long    ProfileId             { get; init; }
    public string  ProfileType           { get; init; } = default!;
    public string  Category              { get; init; } = default!;
    public decimal RawScore              { get; init; }
    public decimal Weight                { get; init; }
    public decimal WeightedContribution  { get; init; }
    public int     MetricCount           { get; init; }
    public string? MetricsJson           { get; init; }
}

// ── Score History ─────────────────────────────────────────────────────────────

public sealed class ProfileScoreHistoryBffDto
{
    public long    Id              { get; init; }
    public long    ProfileId       { get; init; }
    public string  ProfileType     { get; init; } = default!;
    public decimal OverallScore    { get; init; }
    public string  PriorityTier    { get; init; } = default!;
    public decimal ConfidenceScore { get; init; }
    public int     SampleSize      { get; init; }
    public string? TriggerReason   { get; init; }
    public DateTime RecordedAtUtc  { get; init; }
    public string? MetadataJson    { get; init; }
}

public sealed class ProfileScoreHistoryPagedBffResultDto
{
    public List<ProfileScoreHistoryBffDto> Items      { get; init; } = [];
    public int                             TotalCount { get; init; }
    public int                             Page       { get; init; }
    public int                             PageSize   { get; init; }
}

// ── Decision Logs ─────────────────────────────────────────────────────────────

public sealed class ProfileDecisionLogBffDto
{
    public long    Id               { get; init; }
    public long    ProfileId        { get; init; }
    public string  ProfileType      { get; init; } = default!;
    public string  EventType        { get; init; } = default!;
    public string  EventDescription { get; init; } = default!;
    public string? PreviousTier     { get; init; }
    public string? NewTier          { get; init; }
    public decimal? PreviousScore   { get; init; }
    public decimal? NewScore        { get; init; }
    public string?  ActorUserId     { get; init; }
    public DateTime OccurredAtUtc   { get; init; }
    public string?  MetadataJson    { get; init; }
}

public sealed class ProfileDecisionLogPagedBffResultDto
{
    public List<ProfileDecisionLogBffDto> Items      { get; init; } = [];
    public int                            TotalCount { get; init; }
    public int                            Page       { get; init; }
    public int                            PageSize   { get; init; }
}

// ── Risk Signals ──────────────────────────────────────────────────────────────

public sealed class ProfileRiskSignalBffDto
{
    public long    Id               { get; init; }
    public long    ProfileId        { get; init; }
    public string  ProfileType      { get; init; } = default!;
    public string  Severity         { get; init; } = default!;
    public string  SignalCode       { get; init; } = default!;
    public string  Description      { get; init; } = default!;
    public string? SourceModule     { get; init; }
    public long?   SourceEntityId   { get; init; }
    public DateTime DetectedAtUtc   { get; init; }
    public bool    IsResolved       { get; init; }
    public DateTime? ResolvedAtUtc  { get; init; }
    public string? ResolutionNote   { get; init; }
    public long?   ResolvedByUserId { get; init; }
}

public sealed class ProfileRiskSignalPagedBffResultDto
{
    public List<ProfileRiskSignalBffDto> Items      { get; init; } = [];
    public int                           TotalCount { get; init; }
    public int                           Page       { get; init; }
    public int                           PageSize   { get; init; }
}

// ── Tier Listing ──────────────────────────────────────────────────────────────

public sealed class ProfileSnapshotPagedBffResultDto
{
    public List<ProfilePerformanceSnapshotBffDto> Items      { get; init; } = [];
    public int                                    TotalCount { get; init; }
    public int                                    Page       { get; init; }
    public int                                    PageSize   { get; init; }
}

// ── Request Models ────────────────────────────────────────────────────────────

public sealed class RaiseProfileRiskSignalBffRequest
{
    public string  Severity       { get; init; } = default!;
    public string  SignalCode     { get; init; } = default!;
    public string  Description    { get; init; } = default!;
    public string? SourceModule   { get; init; }
    public long?   SourceEntityId { get; init; }
}

public sealed class ResolveProfileRiskSignalBffRequest
{
    public string? ResolutionNote { get; init; }
}

public sealed class RecalculateProfilePerformanceBffRequest
{
    public string? Reason { get; init; }
}

// ── Recalculate Result ────────────────────────────────────────────────────────

public sealed class RecalculateProfilePerformanceBffResult
{
    public ProfilePerformanceSnapshotBffDto Snapshot     { get; init; } = default!;
    public string                           PreviousTier { get; init; } = default!;
    public string                           NewTier      { get; init; } = default!;
    public bool                             TierChanged  { get; init; }
    public bool                             IsColdStart  { get; init; }
}

// ── Phase 21 — Priority Preview ───────────────────────────────────────────────

/// <summary>
/// BFF request body for POST /api/v1/admin-panel/profile/performance/priority-preview.
/// Phase 21 rule: read-only preview — no assignment or scoring changes.
/// </summary>
public sealed class ProfilePriorityPreviewBffRequest
{
    public List<long> CandidateProfileIds { get; init; } = [];
    public string     Context             { get; init; } = default!;
    public string?    CategoryCode        { get; init; }
    public string?    LocationCode        { get; init; }
    public int        MaxResults          { get; init; } = 10;
    public bool       LogDecision         { get; init; } = false;
}

public sealed class ProfilePriorityPreviewBffResult
{
    public List<ProfilePriorityCandidateBffDto> Items              { get; init; } = [];
    public string                               Context            { get; init; } = default!;
    public DateTime                             GeneratedAtUtc     { get; init; }
    public string                               ExplanationSummary { get; init; } = default!;
    public int                                  RequestedCount     { get; init; }
    public int                                  ResolvedCount      { get; init; }
    public int                                  SkippedCount       { get; init; }
}

public sealed class ProfilePriorityCandidateBffDto
{
    public long    ProfileId                   { get; init; }
    public string  ProfileType                 { get; init; } = default!;
    public decimal OverallScore                { get; init; }
    public string  PriorityTier                { get; init; } = default!;
    public decimal PriorityScore               { get; init; }
    public int     Rank                        { get; init; }
    public bool    IsColdStart                 { get; init; }
    public string  ConfidenceLevel             { get; init; } = default!;
    public int     SampleSize                  { get; init; }
    public bool    HasActiveRiskSignal         { get; init; }
    public string? ActiveRiskSignalMaxSeverity { get; init; }
    public List<ProfilePriorityExplanationFactorBffDto> ExplanationFactors { get; init; } = [];
}

public sealed class ProfilePriorityExplanationFactorBffDto
{
    public string  Factor       { get; init; } = default!;
    public decimal Value        { get; init; }
    public decimal Weight       { get; init; }
    public decimal Contribution { get; init; }
    public bool    IsMvpNeutral { get; init; }
    public string? Note         { get; init; }
}
