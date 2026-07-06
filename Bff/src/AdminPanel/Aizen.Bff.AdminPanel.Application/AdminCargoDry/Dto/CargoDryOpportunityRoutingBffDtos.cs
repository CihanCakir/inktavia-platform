using Aizen.Bff.AdminPanel.Application.AdminProfilePerformance.Dto;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Dto;

// ── Remote call result (mirrors module GetCargoDryOpportunityRoutingCandidatesResult) ──

/// <summary>
/// Phase 24 — BFF-side mirror of GetCargoDryOpportunityRoutingCandidatesResult.
/// Deserialized from the CargoDry module's opportunity-routing/candidates endpoint.
/// </summary>
[DocumentationInfo("CargoDry opportunity routing candidates BFF result",
    "Phase 24: BFF-side DTO mirroring the CargoDry module query result. " +
    "Contains the union of distinct provider profile IDs from Active ConsignmentAgreements " +
    "and ProviderInventory records. Used as input to the Profile priority-preview engine " +
    "with context 'CargoDryOpportunityRouting'.")]
public sealed class CargoDryOpportunityRoutingCandidatesBffResult
{
    /// <summary>Union-deduplicated list of provider profile IDs.</summary>
    public IReadOnlyList<long> ProviderProfileIds { get; init; } = [];

    /// <summary>Count of IDs sourced from Active ConsignmentAgreements.</summary>
    public int AgreementSourceCount { get; init; }

    /// <summary>Count of IDs sourced from ProviderInventory records.</summary>
    public int InventorySourceCount { get; init; }

    /// <summary>Total unique provider profile IDs after union deduplication.</summary>
    public int TotalUniqueCount { get; init; }
}

// ── Preview request (admin-facing) ───────────────────────────────────────────

/// <summary>
/// Phase 24 — Admin-triggered CargoDry opportunity routing preview request.
/// Decision-support only: no automatic kit allocation, no consignment agreement
/// creation/modification, no stock assignment, no provider enforcement.
/// </summary>
[DocumentationInfo("CargoDry opportunity routing preview BFF request",
    "Phase 24: Admin posts this to trigger a read-only priority-preview over CargoDry providers. " +
    "The BFF resolves candidates from Active ConsignmentAgreements + ProviderInventory, " +
    "then calls the Profile engine with context 'CargoDryOpportunityRouting'. " +
    "No automatic allocation, no enforcement, no scoring changes.")]
public sealed class CargoDryOpportunityRoutingPreviewBffRequest
{
    /// <summary>
    /// Optional admin override. When provided and non-empty, these profile IDs are used
    /// instead of auto-resolving from CargoDry relationships.
    /// </summary>
    public List<long>? CandidateProfileIds { get; init; }

    /// <summary>
    /// When true, providers whose PriorityTier == "Flagged" are included in results.
    /// Default false (Phase 24 Hard Rule #15).
    /// </summary>
    public bool IncludeFlagged { get; init; } = false;

    /// <summary>Maximum candidates returned by the priority-preview engine.</summary>
    public int MaxResults { get; init; } = 10;

    /// <summary>
    /// When true, the Profile engine records a ProfileDecisionLog entry.
    /// Default false (opt-in only).
    /// </summary>
    public bool LogDecision { get; init; } = false;

    /// <summary>Optional product code filter for context-aware scoring (MVP neutral fallback).</summary>
    public string? ProductCode { get; init; }

    /// <summary>Optional location code filter for context-aware scoring (MVP neutral fallback).</summary>
    public string? LocationCode { get; init; }
}

// ── Preview result ────────────────────────────────────────────────────────────

/// <summary>
/// Phase 24 — CargoDry opportunity routing preview result.
/// Wraps Profile priority-preview output with CargoDry-specific context fields.
/// </summary>
[DocumentationInfo("CargoDry opportunity routing preview BFF result",
    "Phase 24: Ranked CargoDry provider candidates with explanation factors. " +
    "Candidates sourced from Active ConsignmentAgreements + ProviderInventory union. " +
    "AgreementSourceCount and InventorySourceCount trace the candidate origin. " +
    "Items contain ProfilePriorityCandidateBffDto with full explanation factors.")]
public sealed class CargoDryOpportunityRoutingPreviewBffResult
{
    /// <summary>Total candidates sent to the priority-preview engine.</summary>
    public int RequestedCandidateCount { get; init; }

    /// <summary>Candidates for which a snapshot was found and scored.</summary>
    public int ResolvedCandidateCount { get; init; }

    /// <summary>Candidates skipped due to missing snapshots (cold-start visible with warning).</summary>
    public int SkippedCandidateCount { get; init; }

    /// <summary>Providers excluded post-filter because PriorityTier == "Flagged" and IncludeFlagged == false.</summary>
    public int FlaggedExcludedCount { get; init; }

    /// <summary>IDs sourced from Active ConsignmentAgreements (before union).</summary>
    public int AgreementSourceCount { get; init; }

    /// <summary>IDs sourced from ProviderInventory (before union).</summary>
    public int InventorySourceCount { get; init; }

    /// <summary>
    /// Describes how candidates were determined:
    /// "CargoDryActiveRelationships" (auto-resolved) or "AdminOverride".
    /// </summary>
    public string CandidateSource { get; init; } = default!;

    /// <summary>Optional product code passed through from request.</summary>
    public string? ProductCode { get; init; }

    /// <summary>Optional location code passed through from request.</summary>
    public string? LocationCode { get; init; }

    /// <summary>Engine-generated human-readable explanation of the scoring formula used.</summary>
    public string ExplanationSummary { get; init; } = default!;

    /// <summary>UTC timestamp when this preview was generated.</summary>
    public DateTime GeneratedAtUtc { get; init; }

    /// <summary>
    /// Ranked candidate list — each item includes OverallScore, PriorityTier,
    /// ExplanationFactors, and risk signal flags.
    /// Reuses ProfilePriorityCandidateBffDto (Phase 21 type) without modification.
    /// </summary>
    public List<ProfilePriorityCandidateBffDto> Items { get; init; } = [];
}

// ── Response envelope ─────────────────────────────────────────────────────────

/// <summary>
/// Phase 24 — Query handler response envelope.
/// Wraps the preview result with a Warnings collection so partial failures
/// from unavailable modules (CargoDry module, Profile module) are surfaced to the admin UI.
/// </summary>
[DocumentationInfo("CargoDry opportunity routing preview BFF response",
    "Phase 24: Envelope returned by the BFF query handler. " +
    "Data is null and Warnings is populated when either the CargoDry module or " +
    "the Profile performance module is unreachable.")]
public sealed class CargoDryOpportunityRoutingPreviewBffResponse
{
    public CargoDryOpportunityRoutingPreviewBffResult? Data     { get; set; }
    public List<AdminBffWarning>                       Warnings { get; set; } = new();
}
