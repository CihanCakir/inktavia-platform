using Aizen.Bff.AdminPanel.Application.CargoDry.Dto;
using Aizen.Bff.AdminPanel.Application.ProfilePerformance.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Query.GetCargoDryOpportunityRoutingPreview;

/// <summary>
/// Phase 24 BFF orchestration handler — CargoDry opportunity routing priority preview.
///
/// Orchestration steps:
///   1. If request.RequestBody.CandidateProfileIds is non-empty → use AdminOverride.
///   2. Otherwise call CargoDry module: GET /api/v1/cargodry/admin/opportunity-routing/candidates
///      to auto-resolve provider profile IDs from Active ConsignmentAgreements + ProviderInventory.
///   3. If no candidates resolved → return empty result with explanation note.
///   4. Call Profile performance priority-preview engine with context "CargoDryOpportunityRouting".
///   5. Post-filter items with PriorityTier == "Flagged" when IncludeFlagged == false.
///   6. Return CargoDryOpportunityRoutingPreviewBffResponse.
///
/// Phase 24 hard rules enforced here:
///   - No automatic kit allocation (Rule 1)
///   - No automatic ConsignmentAgreement creation/modification (Rule 2)
///   - No automatic stock assignment (Rule 3)
///   - No automatic provider inventory changes (Rule 4)
///   - No CargoDry settlement/payout/commission logic changes (Rules 5–8)
///   - No enforcement based on performance score (Rule 9)
///   - No provider blocking/demotion/penalization (Rule 10)
///   - No participant enforcement (Rule 11)
///   - BFF orchestration/proxy only (Rule 12)
///   - Admin Web → AdminPanel BFF only (Rule 13)
///   - Explanation factors required in output — provided by Profile engine items (Rule 14)
///   - Flagged providers excluded by default (Rule 15)
///   - Cold-start providers visible — engine returns them with low-confidence warning; we do not exclude (Rule 16)
///   - No scoring formula changes (Rule 18)
/// </summary>
[DocumentationInfo("Get CargoDry opportunity routing preview BFF query handler",
    "Phase 24: Orchestrates CargoDry candidates fetch + Profile priority-preview call. " +
    "Resolves provider profile IDs from Active ConsignmentAgreements + ProviderInventory union. " +
    "Calls Profile engine with context 'CargoDryOpportunityRouting'. " +
    "Filters Flagged providers by default. Decision-support only — no allocation or enforcement.")]
public sealed class GetCargoDryOpportunityRoutingPreviewBffQueryHandler
    : AizenQueryHandler<GetCargoDryOpportunityRoutingPreviewBffQuery, CargoDryOpportunityRoutingPreviewBffResponse>
{
    private readonly ICargoDryRemoteCall         _cargoDry;
    private readonly IProfilePerformanceRemoteCall _profile;

    public GetCargoDryOpportunityRoutingPreviewBffQueryHandler(
        ICargoDryRemoteCall          cargoDry,
        IProfilePerformanceRemoteCall profile)
    {
        _cargoDry = cargoDry;
        _profile  = profile;
    }

    public override async Task<CargoDryOpportunityRoutingPreviewBffResponse?> Handle(
        GetCargoDryOpportunityRoutingPreviewBffQuery request,
        CancellationToken cancellationToken)
    {
        var response = new CargoDryOpportunityRoutingPreviewBffResponse();
        var req      = request.RequestBody;

        // ── Step 1: Determine candidate profile IDs ────────────────────────────
        List<long> candidateIds;
        string     candidateSource;
        int        agreementSourceCount = 0;
        int        inventorySourceCount = 0;

        if (req.CandidateProfileIds is { Count: > 0 } overrideIds)
        {
            // Admin-supplied override — skip module call entirely.
            candidateIds    = overrideIds;
            candidateSource = "AdminOverride";
        }
        else
        {
            // ── Step 2: Auto-resolve from CargoDry module ──────────────────────
            CargoDryOpportunityRoutingCandidatesBffResult candidatesResult;

            try
            {
                candidatesResult = await _cargoDry.GetCargoDryOpportunityRoutingCandidatesAsync(cancellationToken);
            }
            catch
            {
                response.Warnings.Add(AdminBffWarning.ModuleUnavailable("CargoDry"));
                return response;
            }

            candidateIds         = candidatesResult.ProviderProfileIds.ToList();
            agreementSourceCount = candidatesResult.AgreementSourceCount;
            inventorySourceCount = candidatesResult.InventorySourceCount;
            candidateSource      = "CargoDryActiveRelationships";
        }

        // ── Step 3: Empty candidates → return empty result ─────────────────────
        if (candidateIds.Count == 0)
        {
            response.Data = new CargoDryOpportunityRoutingPreviewBffResult
            {
                CandidateSource         = candidateSource,
                AgreementSourceCount    = agreementSourceCount,
                InventorySourceCount    = inventorySourceCount,
                RequestedCandidateCount = 0,
                ResolvedCandidateCount  = 0,
                SkippedCandidateCount   = 0,
                FlaggedExcludedCount    = 0,
                ProductCode             = req.ProductCode,
                LocationCode            = req.LocationCode,
                ExplanationSummary      = "No CargoDry candidate providers found. " +
                                          "No active consignment agreements or inventory allocations exist yet.",
                GeneratedAtUtc          = DateTime.UtcNow,
                Items                   = [],
            };
            return response;
        }

        // ── Step 4: Call Profile priority-preview with CargoDryOpportunityRouting context ──
        ProfilePriorityPreviewBffResult previewResult;

        try
        {
            previewResult = await _profile.GetProfilePriorityPreviewAsync(
                new ProfilePriorityPreviewBffRequest
                {
                    CandidateProfileIds = candidateIds,
                    Context             = "CargoDryOpportunityRouting",
                    CategoryCode        = req.ProductCode,   // mapped: ProductCode → CategoryCode slot
                    LocationCode        = req.LocationCode,
                    MaxResults          = req.MaxResults,
                    LogDecision         = req.LogDecision,
                }, cancellationToken);
        }
        catch
        {
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("ProfilePerformance"));
            return response;
        }

        // ── Step 5: Post-filter Flagged providers ──────────────────────────────
        var allItems        = previewResult.Items ?? [];
        var flaggedExcluded = 0;
        List<ProfilePriorityCandidateBffDto> filteredItems;

        if (!req.IncludeFlagged)
        {
            filteredItems   = allItems.Where(i => i.PriorityTier != "Flagged").ToList();
            flaggedExcluded = allItems.Count - filteredItems.Count;
        }
        else
        {
            filteredItems = allItems;
        }

        // ── Step 6: Assemble result ────────────────────────────────────────────
        response.Data = new CargoDryOpportunityRoutingPreviewBffResult
        {
            CandidateSource         = candidateSource,
            AgreementSourceCount    = agreementSourceCount,
            InventorySourceCount    = inventorySourceCount,
            RequestedCandidateCount = previewResult.RequestedCount,
            ResolvedCandidateCount  = previewResult.ResolvedCount,
            SkippedCandidateCount   = previewResult.SkippedCount,
            FlaggedExcludedCount    = flaggedExcluded,
            ProductCode             = req.ProductCode,
            LocationCode            = req.LocationCode,
            ExplanationSummary      = previewResult.ExplanationSummary,
            GeneratedAtUtc          = previewResult.GeneratedAtUtc,
            Items                   = filteredItems,
        };

        return response;
    }
}
