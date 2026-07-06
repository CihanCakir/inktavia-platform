using Aizen.Bff.AdminPanel.Application.AdminProfilePerformance.Dto;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Dto;

// ─── Request ──────────────────────────────────────────────────────────────────

/// <summary>
/// Phase 23 — Admin-only provider recommendation preview request.
/// Decision-support only: no assignment, no offer creation, no provider notification.
/// </summary>
[DocumentationInfo("SR provider recommendation preview BFF request",
    "Phase 23: Admin reads provider candidates from SR offers and calls the Profile priority-preview engine. " +
    "No automatic assignment or enforcement. No provider notifications. BFF orchestration only.")]
public sealed class SrProviderRecommendationPreviewBffRequest
{
    /// <summary>
    /// Optional override. When provided and non-empty, these IDs are used as candidates
    /// instead of extracting them from the SR's submitted offers.
    /// </summary>
    public List<long>? CandidateProfileIds { get; init; }

    /// <summary>
    /// When true, providers whose PriorityTier == "Flagged" are included in the result.
    /// Default false (Phase 23 Hard Rule #14).
    /// </summary>
    public bool IncludeFlagged { get; init; } = false;

    /// <summary>Maximum candidate results returned by the priority-preview engine.</summary>
    public int MaxResults { get; init; } = 10;

    /// <summary>
    /// When true, the priority-preview engine records a ProfileDecisionLog entry.
    /// Default false (Phase 23 rule: opt-in, no automatic decision logging).
    /// </summary>
    public bool LogDecision { get; init; } = false;
}

// ─── Result ───────────────────────────────────────────────────────────────────

/// <summary>
/// Phase 23 — Admin provider recommendation preview result.
/// Wraps ProfilePriorityPreviewBffResult with SR-specific context fields.
/// </summary>
[DocumentationInfo("SR provider recommendation preview BFF result",
    "Phase 23: Ranked provider candidates with explanation factors, sourced from SR offers. " +
    "Includes SR context (category, location), candidate source description, flagged-exclusion count, " +
    "and the ProfilePriorityPreviewBffResult.Items list with full explanation factors.")]
public sealed class SrProviderRecommendationPreviewBffResult
{
    public long    ServiceRequestId     { get; init; }
    public string? ServiceCategoryCode  { get; init; }
    public string? LocationCode         { get; init; }

    /// <summary>
    /// Describes how candidates were determined: "ServiceRequestOffers" or "AdminOverride".
    /// </summary>
    public string CandidateSource { get; init; } = default!;

    public int RequestedCandidateCount { get; init; }
    public int ResolvedCandidateCount  { get; init; }
    public int SkippedCandidateCount   { get; init; }

    /// <summary>
    /// Number of providers excluded because PriorityTier == "Flagged" and IncludeFlagged == false.
    /// </summary>
    public int FlaggedExcludedCount { get; init; }

    public string   ExplanationSummary { get; init; } = default!;
    public DateTime GeneratedAtUtc     { get; init; }

    /// <summary>
    /// Ranked candidates — each includes OverallScore, PriorityTier, ExplanationFactors, risk signal flags.
    /// Reuses ProfilePriorityCandidateBffDto (Phase 21 type) without modification.
    /// </summary>
    public List<ProfilePriorityCandidateBffDto> Items { get; init; } = [];
}

// ─── Response wrapper ─────────────────────────────────────────────────────────

[DocumentationInfo("SR provider recommendation preview BFF response",
    "Query handler response envelope. Wraps the result with a Warnings collection " +
    "so partial failures from unavailable modules can be surfaced to the admin UI.")]
public sealed class SrProviderRecommendationPreviewBffResponse
{
    public SrProviderRecommendationPreviewBffResult? Data     { get; set; }
    public List<AdminBffWarning>                     Warnings { get; set; } = new();
}
