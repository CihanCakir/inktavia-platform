using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Profile.Abstraction.Dtos.Performance;
using Aizen.Modules.Profile.Abstraction.Enums.Performance;
using Aizen.Modules.Profile.Domain.Entities.Performance;
using Aizen.Modules.Profile.Domain.Interface.Repository;

namespace Aizen.Modules.Profile.Application.Queries.Performance.GetProfilePriorityPreview;

/// <summary>
/// Phase 21 — Priority Preview Query Handler.
/// Phase 24 — adds CargoDryOpportunityRouting context-specific formula branching.
///
/// Priority formula (Phase 21 MVP — default for SR and AdminAssignment contexts):
///   PriorityScore = OverallScore       * 0.40
///                 + CategoryFitScore   * 0.20   ← neutral 50 (GeoDiscovery/CategoryMatch not yet integrated)
///                 + LocationFitScore   * 0.15   ← neutral 50 (GeoDiscovery not yet integrated)
///                 + CapacityScore      * 0.10   ← neutral 50 (capacity tracking not yet integrated)
///                 + CargoDryScore      * 0.10   ← real value from snapshot
///                 - RiskPenaltyScore   * 0.05   ← real value from snapshot (penalty)
///                 + ManualBoostValue            ← 0 (not implemented in Phase 21)
///
/// Priority formula (Phase 24 — CargoDryOpportunityRouting context):
///   PriorityScore = OverallScore                * 0.30   ← real
///                 + CargoDryScore               * 0.30   ← real
///                 + CargoDryOpportunityFitScore * 0.15   ← neutral 50 (post-MVP)
///                 + InventoryDisciplineScore    * 0.10   ← neutral 50 (post-MVP)
///                 + LocationFitScore            * 0.05   ← neutral 50 (GeoDiscovery post-MVP)
///                 + ConfidenceNormalized        * 0.05   ← real: ConfidenceScore × 100
///                 - RiskPenaltyScore            * 0.05   ← real
///
/// Hard rules (Phase 21/24):
/// - Does NOT change ServiceRequest assignment logic.
/// - Does NOT change provider search ranking.
/// - Does NOT create scoring penalties or payout holds.
/// - All scores come from the existing snapshot — no recalculation.
/// - Decision log entries are append-only.
/// - Phase 24: No CargoDry kit allocation, no consignment/inventory changes.
/// </summary>
[DocumentationInfo("GetProfilePriorityPreviewQueryHandler",
    "Phase 21/24 read-only priority preview. Ranks candidate providers by a priority score " +
    "derived from their existing performance snapshot. Returns explanation factors per candidate. " +
    "Phase 21 (default): CategoryFit, LocationFit, Capacity neutral (50). " +
    "Phase 24 (CargoDryOpportunityRouting): CargoDryScore weighted 0.30, OverallScore 0.30, " +
    "CargoDryOpportunityFitScore+InventoryDiscipline neutral, ConfidenceNormalized live. " +
    "Does NOT modify any entity or trigger any assignment.")]
public sealed class GetProfilePriorityPreviewQueryHandler
    : AizenQueryHandler<GetProfilePriorityPreviewQuery, ProfilePriorityPreviewResultDto>
{
    // ── Phase 21 default formula weights ─────────────────────────────────────
    private const decimal W_OVERALL       = 0.40m;
    private const decimal W_CATEGORY_FIT  = 0.20m;
    private const decimal W_LOCATION_FIT  = 0.15m;
    private const decimal W_CAPACITY      = 0.10m;
    private const decimal W_CARGODRY      = 0.10m;
    private const decimal W_RISK_PENALTY  = 0.05m; // subtracted

    // ── Phase 24 CargoDryOpportunityRouting formula weights ──────────────────
    private const decimal CD_W_OVERALL               = 0.30m;
    private const decimal CD_W_CARGODRY              = 0.30m;
    private const decimal CD_W_OPPORTUNITY_FIT       = 0.15m;
    private const decimal CD_W_INVENTORY_DISCIPLINE  = 0.10m;
    private const decimal CD_W_LOCATION_FIT          = 0.05m;
    private const decimal CD_W_CONFIDENCE            = 0.05m;
    private const decimal CD_W_RISK_PENALTY          = 0.05m; // subtracted

    // ── MVP neutral value (when real data is not available) ──────────────────
    private const decimal MVP_NEUTRAL = 50m;

    private readonly IProfilePerformanceSnapshotRepository _snapshots;
    private readonly IProfileDecisionLogRepository         _decisionLogs;

    public GetProfilePriorityPreviewQueryHandler(
        IProfilePerformanceSnapshotRepository snapshots,
        IProfileDecisionLogRepository         decisionLogs)
    {
        _snapshots    = snapshots;
        _decisionLogs = decisionLogs;
    }

    public override async Task<ProfilePriorityPreviewResultDto> Handle(
        GetProfilePriorityPreviewQuery request, CancellationToken ct)
    {
        // ── 1. Bulk-fetch active snapshots for all candidate IDs ─────────────
        var snapshots = await _snapshots.GetByProfilesAsync(
            request.CandidateProfileIds, ProfileType.Provider, ct);

        var resolvedCount = snapshots.Count;
        var skippedCount  = request.CandidateProfileIds.Count - resolvedCount;

        // ── 2. Compute priority score + explanation for each resolved candidate ─
        var isCargoDryContext = string.Equals(
            request.Context,
            SupportedPriorityPreviewContexts.CargoDryOpportunityRouting,
            StringComparison.OrdinalIgnoreCase);

        var candidates = new List<ProfilePriorityCandidateDto>(resolvedCount);

        foreach (var snapshot in snapshots)
        {
            var candidate = isCargoDryContext
                ? ComputeCandidateCargoDry(snapshot)
                : ComputeCandidate(snapshot);
            candidates.Add(candidate);
        }

        // ── 3. Sort by PriorityScore desc, apply MaxResults, assign rank ────────
        var ranked = candidates
            .OrderByDescending(c => c.PriorityScore)
            .ThenByDescending(c => c.OverallScore)
            .Take(request.MaxResults)
            .ToList();

        for (var i = 0; i < ranked.Count; i++)
            ranked[i].Rank = i + 1;

        // ── 4. Optionally append decision log entry (append-only) ────────────
        if (request.LogDecision && ranked.Count > 0)
        {
            var topCandidate = ranked[0];
            var logEntry = ProfileDecisionLogEntity.Create(
                profileId:        topCandidate.ProfileId,
                profileType:      topCandidate.ProfileType,
                eventType:        DecisionLogEventType.PriorityPreviewGenerated,
                eventDescription: $"Priority preview generated. Context: {request.Context}. " +
                                  $"Candidates: {request.CandidateProfileIds.Count}. " +
                                  $"Resolved: {resolvedCount}. Top profileId: {topCandidate.ProfileId} " +
                                  $"(score: {topCandidate.PriorityScore:F2}).",
                previousTier:     null,
                newTier:          null,
                previousScore:    null,
                newScore:         null,
                actorUserId:      request.ActorUserId,
                metadataJson:     $"{{\"context\":\"{request.Context}\"," +
                                  $"\"candidateCount\":{request.CandidateProfileIds.Count}," +
                                  $"\"resolvedCount\":{resolvedCount}," +
                                  $"\"categoryCode\":\"{request.CategoryCode}\"," +
                                  $"\"locationCode\":\"{request.LocationCode}\"}}");

            await _decisionLogs.AddAsync(logEntry, ct);
            await _decisionLogs.SaveChangesAsync(ct);
        }

        // ── 5. Build result ───────────────────────────────────────────────────
        var summary = BuildExplanationSummary(request, resolvedCount, skippedCount);

        return new ProfilePriorityPreviewResultDto
        {
            Items              = ranked,
            Context            = request.Context,
            GeneratedAtUtc     = DateTime.UtcNow,
            ExplanationSummary = summary,
            RequestedCount     = request.CandidateProfileIds.Count,
            ResolvedCount      = resolvedCount,
            SkippedCount       = skippedCount,
        };
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Phase 21 default formula — used for ServiceRequestProviderRecommendation
    /// and AdminAssignmentSuggestion contexts.
    /// </summary>
    private static ProfilePriorityCandidateDto ComputeCandidate(
        ProfilePerformanceSnapshotEntity snapshot)
    {
        // MVP neutral factors — will be replaced by real data in post-MVP phases
        var categoryFitScore = MVP_NEUTRAL;
        var locationFitScore = MVP_NEUTRAL;
        var capacityScore    = MVP_NEUTRAL;
        var cargoDryScore    = snapshot.CargoDryScore;
        var riskPenalty      = snapshot.RiskPenaltyScore;
        var manualBoost      = 0m;
        var overallScore     = snapshot.OverallScore;

        var priorityScore =
              overallScore     * W_OVERALL
            + categoryFitScore * W_CATEGORY_FIT
            + locationFitScore * W_LOCATION_FIT
            + capacityScore    * W_CAPACITY
            + cargoDryScore    * W_CARGODRY
            - riskPenalty      * W_RISK_PENALTY
            + manualBoost;

        priorityScore = Math.Max(0m, Math.Min(100m, priorityScore));

        var factors = new List<ProfilePriorityExplanationFactorDto>
        {
            new()
            {
                Factor       = "OverallScore",
                Value        = overallScore,
                Weight       = W_OVERALL,
                Contribution = overallScore * W_OVERALL,
                IsMvpNeutral = false,
                Note         = "Composite performance score from snapshot.",
            },
            new()
            {
                Factor       = "CategoryFit",
                Value        = categoryFitScore,
                Weight       = W_CATEGORY_FIT,
                Contribution = categoryFitScore * W_CATEGORY_FIT,
                IsMvpNeutral = true,
                Note         = "MVP neutral (50). Category-based fit scoring deferred post-MVP.",
            },
            new()
            {
                Factor       = "LocationFit",
                Value        = locationFitScore,
                Weight       = W_LOCATION_FIT,
                Contribution = locationFitScore * W_LOCATION_FIT,
                IsMvpNeutral = true,
                Note         = "MVP neutral (50). GeoDiscovery integration deferred post-MVP.",
            },
            new()
            {
                Factor       = "Capacity",
                Value        = capacityScore,
                Weight       = W_CAPACITY,
                Contribution = capacityScore * W_CAPACITY,
                IsMvpNeutral = true,
                Note         = "MVP neutral (50). Capacity tracking deferred post-MVP.",
            },
            new()
            {
                Factor       = "CargoDryContribution",
                Value        = cargoDryScore,
                Weight       = W_CARGODRY,
                Contribution = cargoDryScore * W_CARGODRY,
                IsMvpNeutral = false,
                Note         = "CargoDry dimension score from snapshot.",
            },
            new()
            {
                Factor       = "RiskPenalty",
                Value        = riskPenalty,
                Weight       = W_RISK_PENALTY,
                Contribution = -(riskPenalty * W_RISK_PENALTY),
                IsMvpNeutral = false,
                Note         = snapshot.HasActiveRiskSignal
                    ? $"Active risk signal ({snapshot.ActiveRiskSignalMaxSeverity}) — penalty applied."
                    : "No active risk signal.",
            },
        };

        return BuildCandidateDto(snapshot, priorityScore, factors);
    }

    /// <summary>
    /// Phase 24 formula — used for CargoDryOpportunityRouting context.
    /// CargoDryScore and OverallScore are each weighted 0.30 (CargoDry-heavy).
    /// CargoDryOpportunityFitScore, InventoryDisciplineScore = neutral 50 (post-MVP).
    /// LocationFitScore = neutral 50 (GeoDiscovery post-MVP).
    /// ConfidenceNormalized = snapshot.ConfidenceScore × 100 (0–100 scale).
    /// Phase 24 hard rule: No scoring formula changes beyond adding this context branch.
    /// </summary>
    private static ProfilePriorityCandidateDto ComputeCandidateCargoDry(
        ProfilePerformanceSnapshotEntity snapshot)
    {
        var overallScore              = snapshot.OverallScore;
        var cargoDryScore             = snapshot.CargoDryScore;
        var cargoDryOpportunityFit    = MVP_NEUTRAL; // post-MVP: real fit score
        var inventoryDisciplineScore  = MVP_NEUTRAL; // post-MVP: kit activation discipline
        var locationFitScore          = MVP_NEUTRAL; // post-MVP: GeoDiscovery
        var confidenceNormalized      = snapshot.ConfidenceScore * 100m; // 0.0–1.0 → 0–100
        var riskPenalty               = snapshot.RiskPenaltyScore;

        var priorityScore =
              overallScore             * CD_W_OVERALL
            + cargoDryScore            * CD_W_CARGODRY
            + cargoDryOpportunityFit   * CD_W_OPPORTUNITY_FIT
            + inventoryDisciplineScore * CD_W_INVENTORY_DISCIPLINE
            + locationFitScore         * CD_W_LOCATION_FIT
            + confidenceNormalized     * CD_W_CONFIDENCE
            - riskPenalty              * CD_W_RISK_PENALTY;

        priorityScore = Math.Max(0m, Math.Min(100m, priorityScore));

        var factors = new List<ProfilePriorityExplanationFactorDto>
        {
            new()
            {
                Factor       = "OverallScore",
                Value        = overallScore,
                Weight       = CD_W_OVERALL,
                Contribution = overallScore * CD_W_OVERALL,
                IsMvpNeutral = false,
                Note         = "Composite performance score from snapshot.",
            },
            new()
            {
                Factor       = "CargoDryScore",
                Value        = cargoDryScore,
                Weight       = CD_W_CARGODRY,
                Contribution = cargoDryScore * CD_W_CARGODRY,
                IsMvpNeutral = false,
                Note         = "CargoDry dimension score from snapshot. Heavily weighted for CargoDry routing.",
            },
            new()
            {
                Factor       = "CargoDryOpportunityFit",
                Value        = cargoDryOpportunityFit,
                Weight       = CD_W_OPPORTUNITY_FIT,
                Contribution = cargoDryOpportunityFit * CD_W_OPPORTUNITY_FIT,
                IsMvpNeutral = true,
                Note         = "MVP neutral (50). Kit activation fit scoring deferred post-MVP.",
            },
            new()
            {
                Factor       = "InventoryDiscipline",
                Value        = inventoryDisciplineScore,
                Weight       = CD_W_INVENTORY_DISCIPLINE,
                Contribution = inventoryDisciplineScore * CD_W_INVENTORY_DISCIPLINE,
                IsMvpNeutral = true,
                Note         = "MVP neutral (50). Inventory replenishment discipline deferred post-MVP.",
            },
            new()
            {
                Factor       = "LocationFit",
                Value        = locationFitScore,
                Weight       = CD_W_LOCATION_FIT,
                Contribution = locationFitScore * CD_W_LOCATION_FIT,
                IsMvpNeutral = true,
                Note         = "MVP neutral (50). GeoDiscovery integration deferred post-MVP.",
            },
            new()
            {
                Factor       = "ConfidenceNormalized",
                Value        = confidenceNormalized,
                Weight       = CD_W_CONFIDENCE,
                Contribution = confidenceNormalized * CD_W_CONFIDENCE,
                IsMvpNeutral = false,
                Note         = $"Provider confidence score normalized to 0–100 (raw: {snapshot.ConfidenceScore:F2}). " +
                               (confidenceNormalized < 25m
                                   ? "Low confidence — cold-start or limited activity."
                                   : "Sufficient confidence level."),
            },
            new()
            {
                Factor       = "RiskPenalty",
                Value        = riskPenalty,
                Weight       = CD_W_RISK_PENALTY,
                Contribution = -(riskPenalty * CD_W_RISK_PENALTY),
                IsMvpNeutral = false,
                Note         = snapshot.HasActiveRiskSignal
                    ? $"Active risk signal ({snapshot.ActiveRiskSignalMaxSeverity}) — penalty applied."
                    : "No active risk signal.",
            },
        };

        return BuildCandidateDto(snapshot, priorityScore, factors);
    }

    private static ProfilePriorityCandidateDto BuildCandidateDto(
        ProfilePerformanceSnapshotEntity       snapshot,
        decimal                                priorityScore,
        List<ProfilePriorityExplanationFactorDto> factors)
    {
        var isColdStart = snapshot.MetadataJson?.Contains("\"coldStart\":true") ?? false;

        return new ProfilePriorityCandidateDto
        {
            ProfileId                   = snapshot.ProfileId,
            ProfileType                 = snapshot.ProfileType,
            OverallScore                = snapshot.OverallScore,
            PriorityTier                = snapshot.PriorityTier,
            PriorityScore               = priorityScore,
            Rank                        = 0, // assigned after sorting
            IsColdStart                 = isColdStart,
            ConfidenceLevel             = snapshot.ConfidenceLevel,
            SampleSize                  = snapshot.SampleSize,
            HasActiveRiskSignal         = snapshot.HasActiveRiskSignal,
            ActiveRiskSignalMaxSeverity = snapshot.ActiveRiskSignalMaxSeverity?.ToString(),
            ExplanationFactors          = factors,
        };
    }

    private static string BuildExplanationSummary(
        GetProfilePriorityPreviewQuery request,
        int resolvedCount,
        int skippedCount)
    {
        var parts = new List<string>
        {
            $"Context: {request.Context}.",
            $"Candidates requested: {request.CandidateProfileIds.Count}.",
            $"Snapshots resolved: {resolvedCount}.",
        };

        if (skippedCount > 0)
            parts.Add($"Skipped (no snapshot): {skippedCount}.");

        var formulaLine = string.Equals(
            request.Context,
            SupportedPriorityPreviewContexts.CargoDryOpportunityRouting,
            StringComparison.OrdinalIgnoreCase)
            ? "Formula (CargoDryOpportunityRouting): OverallScore×0.30 + CargoDryScore×0.30 + " +
              "CargoDryOpportunityFit×0.15 + InventoryDiscipline×0.10 + LocationFit×0.05 + " +
              "ConfidenceNormalized×0.05 − RiskPenalty×0.05. " +
              "MVP: CargoDryOpportunityFit, InventoryDiscipline, LocationFit are neutral (50)."
            : "Formula: OverallScore×0.40 + CategoryFit×0.20 + LocationFit×0.15 + " +
              "Capacity×0.10 + CargoDry×0.10 − RiskPenalty×0.05. " +
              "MVP: CategoryFit, LocationFit, Capacity are neutral (50).";

        parts.Add(formulaLine);
        parts.Add("Phase 21/24 rule: read-only preview — no assignment or scoring changes.");

        return string.Join(" ", parts);
    }
}
