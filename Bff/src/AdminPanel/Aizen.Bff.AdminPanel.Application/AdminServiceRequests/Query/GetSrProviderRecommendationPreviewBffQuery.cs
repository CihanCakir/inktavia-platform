using Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Query;

/// <summary>
/// Phase 23 — BFF orchestration query: provider recommendation preview for a ServiceRequest.
/// The handler fetches SR offers, extracts provider profile IDs, and calls the
/// Profile priority-preview engine. Decision-support only — no assignment, no notifications.
/// </summary>
public sealed class GetSrProviderRecommendationPreviewBffQuery
    : AizenQuery<SrProviderRecommendationPreviewBffResponse>
{
    public long                                    ServiceRequestId { get; }
    public SrProviderRecommendationPreviewBffRequest RequestBody    { get; }

    public GetSrProviderRecommendationPreviewBffQuery(
        long                                    serviceRequestId,
        SrProviderRecommendationPreviewBffRequest requestBody)
    {
        ServiceRequestId = serviceRequestId;
        RequestBody      = requestBody;
    }
}
