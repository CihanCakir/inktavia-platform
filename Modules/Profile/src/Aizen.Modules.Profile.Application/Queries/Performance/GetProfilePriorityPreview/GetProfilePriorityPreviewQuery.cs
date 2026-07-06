using Aizen.Core.CQRS.Message;
using Aizen.Modules.Profile.Abstraction.Dtos.Performance;

namespace Aizen.Modules.Profile.Application.Queries.Performance.GetProfilePriorityPreview;

/// <summary>
/// Phase 21 — Read-only priority preview query.
/// Returns a ranked list of provider candidates with priority scores and explanation factors.
///
/// Hard rules (Phase 21):
/// - Does NOT change ServiceRequest assignment logic.
/// - Does NOT change provider search ranking.
/// - Does NOT create scoring penalties or payout holds.
/// - All scores come from the existing ProfilePerformanceSnapshot — no recalculation.
/// - BFF must remain proxy-only for this endpoint.
/// - Admin-only access.
/// </summary>
public sealed class GetProfilePriorityPreviewQuery : AizenQuery<ProfilePriorityPreviewResultDto>
{
    public List<long> CandidateProfileIds { get; init; } = [];

    /// <summary>
    /// Supported (MVP): ServiceRequestProviderRecommendation | AdminAssignmentSuggestion.
    /// Deferred: CargoDryOpportunityRouting | ProviderSearchRanking.
    /// </summary>
    public string Context { get; init; } = default!;

    /// <summary>Optional category code — Phase 21 MVP: neutral 50 applied when null.</summary>
    public string? CategoryCode { get; init; }

    /// <summary>Optional location code — Phase 21 MVP: neutral 50 applied when null.</summary>
    public string? LocationCode { get; init; }

    public int  MaxResults   { get; init; } = 10;
    public bool LogDecision  { get; init; } = false;
    public string? ActorUserId { get; init; }
}
