using Aizen.Bff.AdminPanel.Application.ProfilePerformance.Dto;
using Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Offer;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Query;

/// <summary>
/// Phase 23 BFF orchestration handler.
///
/// Orchestration steps:
///   1. Parallel: fetch SR offers + SR detail from ServiceRequest module.
///   2. Extract candidate provider profile IDs from offers (ProviderOfferItemDto.ProviderId → long).
///   3. If request supplies CandidateProfileIds override (non-empty), use those instead.
///   4. If no candidates resolved → return empty result with explanation note.
///   5. Call Profile performance priority-preview with context "ServiceRequestProviderRecommendation".
///   6. Post-filter items with PriorityTier == "Flagged" when IncludeFlagged == false.
///   7. Return SrProviderRecommendationPreviewBffResult.
///
/// Phase 23 hard rules enforced here:
///   - No automatic provider assignment (Rule 1)
///   - No SR state machine changes (Rule 2)
///   - No offer creation logic changes (Rule 3)
///   - No provider notifications (Rule 4)
///   - BFF orchestration only; no score calculation (Rule 10)
///   - AdminPanel BFF only (Rule 11)
///   - Explanation factors always included via Items (Rule 12)
///   - Cold-start providers remain visible — engine returns them; we do not exclude (Rule 13)
///   - Flagged providers excluded by default unless IncludeFlagged == true (Rule 14)
///   - No scoring formula changes (Rule 16)
/// </summary>
[DocumentationInfo("Get SR provider recommendation preview BFF query handler",
    "Phase 23: Orchestrates SR offers fetch + Profile priority-preview call. " +
    "Extracts ProviderOfferItemDto.ProviderId (string) → long for CandidateProfileIds. " +
    "Filters Flagged providers by default. Decision-support only — no assignment or notifications.")]
public sealed class GetSrProviderRecommendationPreviewBffQueryHandler
    : AizenQueryHandler<GetSrProviderRecommendationPreviewBffQuery, SrProviderRecommendationPreviewBffResponse>
{
    private readonly IServiceRequestRemoteCall     _serviceRequest;
    private readonly IProfilePerformanceRemoteCall _profile;

    public GetSrProviderRecommendationPreviewBffQueryHandler(
        IServiceRequestRemoteCall     serviceRequest,
        IProfilePerformanceRemoteCall profile)
    {
        _serviceRequest = serviceRequest;
        _profile        = profile;
    }

    public override async Task<SrProviderRecommendationPreviewBffResponse?> Handle(
        GetSrProviderRecommendationPreviewBffQuery request,
        CancellationToken cancellationToken)
    {
        var response = new SrProviderRecommendationPreviewBffResponse();

        // ── Step 1: Parallel fetch — SR offers + SR detail ─────────────────────
        Task<AizenApiResponse<GetProviderOffersResponse>>    offersTask;
        Task<AizenApiResponse<GetServiceRequestDetailResponse>> detailTask;

        try
        {
            offersTask = _serviceRequest.GetAdminServiceRequestOffers(request.ServiceRequestId);
            detailTask = _serviceRequest.GetAdminServiceRequestDetail(request.ServiceRequestId);
            await Task.WhenAll(offersTask, detailTask);
        }
        catch
        {
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("ServiceRequest"));
            return response;
        }

        // ── Step 2: Extract SR context (category + location) ───────────────────
        string? categoryCode = null;
        string? locationCode = null;

        try
        {
            var srDto    = detailTask.Result?.Body?.Detail?.Request;
            categoryCode = srDto?.ServiceCategoryCode;
            locationCode = srDto?.LocationCityCode ?? srDto?.LocationCountryCode;
        }
        catch
        {
            // Optional context enrichment — swallow and continue without it.
        }

        // ── Step 3: Determine candidate profile IDs ────────────────────────────
        List<long> candidateIds;
        string     candidateSource;

        if (request.RequestBody.CandidateProfileIds is { Count: > 0 } overrideIds)
        {
            candidateIds    = overrideIds;
            candidateSource = "AdminOverride";
        }
        else
        {
            try
            {
                var offers = offersTask.Result?.Body?.Offers ?? [];
                candidateIds = offers
                    .Where(o => !string.IsNullOrWhiteSpace(o.ProviderId))
                    .Select(o => long.TryParse(o.ProviderId, out var pid) ? pid : 0L)
                    .Where(id => id > 0)
                    .Distinct()
                    .ToList();

                candidateSource = "ServiceRequestOffers";
            }
            catch
            {
                response.Warnings.Add(AdminBffWarning.ModuleUnavailable("ServiceRequest"));
                return response;
            }
        }

        // ── Step 4: Empty candidates → return empty result ─────────────────────
        if (candidateIds.Count == 0)
        {
            response.Data = new SrProviderRecommendationPreviewBffResult
            {
                ServiceRequestId        = request.ServiceRequestId,
                ServiceCategoryCode     = categoryCode,
                LocationCode            = locationCode,
                CandidateSource         = candidateSource,
                RequestedCandidateCount = 0,
                ResolvedCandidateCount  = 0,
                SkippedCandidateCount   = 0,
                FlaggedExcludedCount    = 0,
                ExplanationSummary      = "No candidate providers found. Awaiting offers on this service request.",
                GeneratedAtUtc          = DateTime.UtcNow,
                Items                   = [],
            };
            return response;
        }

        // ── Step 5: Call Profile priority-preview ──────────────────────────────
        ProfilePriorityPreviewBffResult previewResult;

        try
        {
            previewResult = await _profile.GetProfilePriorityPreviewAsync(
                new ProfilePriorityPreviewBffRequest
                {
                    CandidateProfileIds = candidateIds,
                    Context             = "ServiceRequestProviderRecommendation",
                    CategoryCode        = categoryCode,
                    LocationCode        = locationCode,
                    MaxResults          = request.RequestBody.MaxResults,
                    LogDecision         = request.RequestBody.LogDecision,
                }, cancellationToken);
        }
        catch
        {
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("ProfilePerformance"));
            return response;
        }

        // ── Step 6: Post-filter Flagged providers ──────────────────────────────
        var allItems        = previewResult.Items ?? [];
        var flaggedExcluded = 0;
        List<ProfilePriorityCandidateBffDto> filteredItems;

        if (!request.RequestBody.IncludeFlagged)
        {
            filteredItems   = allItems.Where(i => i.PriorityTier != "Flagged").ToList();
            flaggedExcluded = allItems.Count - filteredItems.Count;
        }
        else
        {
            filteredItems = allItems;
        }

        // ── Step 7: Assemble result ────────────────────────────────────────────
        response.Data = new SrProviderRecommendationPreviewBffResult
        {
            ServiceRequestId        = request.ServiceRequestId,
            ServiceCategoryCode     = categoryCode,
            LocationCode            = locationCode,
            CandidateSource         = candidateSource,
            RequestedCandidateCount = previewResult.RequestedCount,
            ResolvedCandidateCount  = previewResult.ResolvedCount,
            SkippedCandidateCount   = previewResult.SkippedCount,
            FlaggedExcludedCount    = flaggedExcluded,
            ExplanationSummary      = previewResult.ExplanationSummary,
            GeneratedAtUtc          = previewResult.GeneratedAtUtc,
            Items                   = filteredItems,
        };

        return response;
    }
}
