using Aizen.Core.Infrastructure.Api;
using Aizen.Core.RemoteCall.Abstraction;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Filter;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Offer;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Jobs;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Offer;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Provider;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;
namespace Aizen.Bff.MarineProvider.Application.Common.RemoteClients;

/// <summary>
/// BFF -> ServiceRequest module calls (provider-scoped). Auth (Keycloak service token) + the trusted-BFF
/// identity assertion headers are injected automatically by <c>MarineProviderBffAuthDelegatingHandler</c>.
/// </summary>
public interface IServiceRequestRemoteCall : IAizenRemoteCall
{
    [AizenRemoteCallGet("/api/v1/service-requests/provider/jobs")]
    Task<AizenApiResponse<GetProviderJobsResponse>> GetProviderJobs(
        [Refit.Query] int pageIndex = 0,
        [Refit.Query] int pageSize = 50);

    [AizenRemoteCallGet("/api/v1/service-requests/provider/jobs/summary")]
    Task<AizenApiResponse<GetProviderJobsSummaryResponse>> GetProviderJobsSummary();

    [AizenRemoteCallGet("/api/v1/service-requests/provider/jobs/workload")]
    Task<AizenApiResponse<GetProviderJobsWorkloadResponse>> GetProviderJobsWorkload([Refit.Query] int weeks = 6);

    [AizenRemoteCallGet("/api/v1/service-requests/provider/jobs/action-required")]
    Task<AizenApiResponse<GetProviderJobsActionRequiredResponse>> GetProviderJobsActionRequired();

    [AizenRemoteCallGet("/api/v1/service-requests/provider/jobs/{assignmentId}")]
    Task<AizenApiResponse<GetProviderJobDetailResponse>> GetProviderJobDetail(long assignmentId);

    [AizenRemoteCallPost("/api/v1/service-requests/provider/jobs/{assignmentId}/start")]
    Task StartJob(long assignmentId);

    [AizenRemoteCallPost("/api/v1/service-requests/provider/jobs/{assignmentId}/complete")]
    Task CompleteJob(long assignmentId,
        [AizenRemoteCallBody] Aizen.Modules.ServiceRequest.Abstraction.Request.Completion.SubmitServiceRequestCompletionRequest body);

    // N-E — provider rejects an assigned job with a structured reason + optional note.
    [AizenRemoteCallPost("/api/v1/service-requests/provider/jobs/{assignmentId}/reject")]
    Task RejectJob(long assignmentId,
        [AizenRemoteCallBody] Aizen.Modules.ServiceRequest.Abstraction.Request.Assignment.RejectServiceRequestAssignmentRequest body);

    [AizenRemoteCallGet("/api/v1/service-requests/provider/jobs/{assignmentId}/work-logs")]
    Task<AizenApiResponse<Aizen.Modules.ServiceRequest.Abstraction.Response.WorkLog.GetServiceRequestWorkLogsResponse>> GetWorkLogs(long assignmentId);

    [AizenRemoteCallPost("/api/v1/service-requests/provider/jobs/{assignmentId}/work-logs")]
    Task<AizenApiResponse<Aizen.Modules.ServiceRequest.Abstraction.Response.WorkLog.AddServiceRequestWorkLogResponse>> AddWorkLog(
        long assignmentId, [AizenRemoteCallBody] Aizen.Modules.ServiceRequest.Abstraction.Request.WorkLog.AddServiceRequestWorkLogRequest body);

    [AizenRemoteCallGet("/api/v1/service-requests/provider/open")]
    Task<AizenApiResponse<GetOpenServiceRequestsResponse>> GetOpenServiceRequests(
        [Refit.Query] int pageIndex = 0,
        [Refit.Query] int pageSize = 20,
        [Refit.Query] string? serviceCategoryCode = null,
        [Refit.Query] string? locationCityCode = null,
        [Refit.Query] string? locationCountryCode = null,
        [Refit.Query] string? searchTerm = null);

    [AizenRemoteCallGet("/api/v1/service-requests/provider/my-offers")]
    Task<AizenApiResponse<GetMyOffersResponse>> GetMyOffers(
        [Refit.Query] int pageIndex = 0,
        [Refit.Query] int pageSize = 20,
        [Refit.Query] int? status = null);

    /// <summary>
    /// The PROVIDER-scoped detail endpoint. The plain `/api/v1/service-requests/{id}` route is the customer's and
    /// has no provider access check — pointing the BFF at it would let a provider walk the id space and read every
    /// customer's request. This route rejects a request the calling provider has no relationship with.
    /// </summary>
    [AizenRemoteCallGet("/api/v1/service-requests/provider/service-requests/{serviceRequestId}")]
    Task<AizenApiResponse<GetProviderServiceRequestDetailResponse>> GetServiceRequestDetail(
        long serviceRequestId,
        [Refit.Query] decimal? centerLatitude = null,
        [Refit.Query] decimal? centerLongitude = null);

    [AizenRemoteCallPost("/api/v1/service-requests/{serviceRequestId}/offers")]
    Task<AizenApiResponse<CreateServiceRequestOfferResponse>> CreateOffer(
        long serviceRequestId,
        [AizenRemoteCallBody] CreateServiceRequestOfferRequest body);

    [AizenRemoteCallPut("/api/v1/service-requests/{serviceRequestId}/offers/{offerId}")]
    Task<AizenApiResponse<UpdateServiceRequestOfferResponse>> UpdateOffer(
        long serviceRequestId, long offerId,
        [AizenRemoteCallBody] UpdateServiceRequestOfferRequest body);

    [AizenRemoteCallPatch("/api/v1/service-requests/{serviceRequestId}/offers/{offerId}/withdraw")]
    Task<AizenApiResponse<WithdrawServiceRequestOfferResponse>> WithdrawOffer(
        long serviceRequestId, long offerId,
        [AizenRemoteCallBody] WithdrawServiceRequestOfferRequest body);

    // BE_WC3d — removed GetMessages remote-call (SR chat READ): the provider Request-detail page now reads the unified
    // Messaging thread (/provider/messaging/service-requests/{id}/thread). sr.Messages has no chat reader. The POST send
    // below stays (SR write path retained for the WC2 flag-OFF path until WC4).
    [AizenRemoteCallPost("/api/v1/service-requests/{serviceRequestId}/messages")]
    Task<AizenApiResponse<Aizen.Modules.ServiceRequest.Abstraction.Response.Message.SendServiceRequestMessageResponse>> SendMessage(
        long serviceRequestId,
        [AizenRemoteCallBody] Aizen.Modules.ServiceRequest.Abstraction.Request.Message.SendServiceRequestMessageRequest body);

    [AizenRemoteCallGet("/api/v1/service-requests/provider/service-requests/{serviceRequestId}/attachments/{fileId}/access-check")]
    Task<AizenApiResponse<GetAttachmentAccessCheckResponse>> CheckAttachmentAccess(long serviceRequestId, Guid fileId);

    [AizenRemoteCallPut("/api/v1/service-requests/{serviceRequestId}/offers/draft")]
    Task<AizenApiResponse<SaveOfferDraftResponse>> SaveOfferDraft(
        long serviceRequestId,
        [AizenRemoteCallBody] SaveOfferDraftRequest body);

    [AizenRemoteCallPost("/api/v1/service-requests/{serviceRequestId}/offers/preview")]
    Task<AizenApiResponse<PreviewOfferResponse>> PreviewOffer(
        long serviceRequestId,
        [AizenRemoteCallBody] SaveOfferDraftRequest body);

    [AizenRemoteCallPatch("/api/v1/service-requests/{serviceRequestId}/offers/{offerId}/submit")]
    Task<AizenApiResponse<SubmitOfferResponse>> SubmitOffer(
        long serviceRequestId, long offerId,
        [AizenRemoteCallBody] SubmitOfferRequest body);

    // --- S2 pricing attributes (provider) ---
    [AizenRemoteCallGet("/api/v1/service-requests/provider/service-requests/{serviceRequestId}/applicable-pricing-attributes")]
    Task<AizenApiResponse<List<Aizen.Modules.ServiceRequest.Abstraction.Dto.Pricing.ApplicablePricingAttributeDto>>> GetApplicablePricingAttributes(long serviceRequestId);

    [AizenRemoteCallGet("/api/v1/service-requests/provider/offers/{offerId}/items/{itemId}/attributes")]
    Task<AizenApiResponse<List<Aizen.Modules.ServiceRequest.Abstraction.Dto.Pricing.PricingAttributeValueDto>>> GetOfferLineAttributes(long offerId, long itemId);

    [AizenRemoteCallPut("/api/v1/service-requests/provider/offers/{offerId}/items/{itemId}/attributes")]
    Task<AizenApiResponse<List<Aizen.Modules.ServiceRequest.Abstraction.Dto.Pricing.PricingAttributeValueDto>>> SetOfferLineAttributes(
        long offerId, long itemId,
        [AizenRemoteCallBody] Aizen.Modules.ServiceRequest.Abstraction.Request.Pricing.SetOfferLineAttributesRequest body);

    // --- S4 travel pricing (provider) ---
    [AizenRemoteCallGet("/api/v1/service-requests/provider/offers/{offerId}/items/{itemId}/travel-pricing")]
    Task<AizenApiResponse<Aizen.Modules.ServiceRequest.Abstraction.Dto.Travel.TravelPricingDetailDto>> GetOfferLineTravelPricing(long offerId, long itemId);

    [AizenRemoteCallPut("/api/v1/service-requests/provider/offers/{offerId}/items/{itemId}/travel-pricing")]
    Task<AizenApiResponse<Aizen.Modules.ServiceRequest.Abstraction.Dto.Travel.TravelPricingDetailDto>> SetOfferLineTravelPricing(
        long offerId, long itemId,
        [AizenRemoteCallBody] Aizen.Modules.ServiceRequest.Abstraction.Request.Travel.SetOfferLineTravelPricingRequest body);

    // --- S5 part-terms allowance preview (cost-free; never carries supplier cost / dealer margin) ---
    [AizenRemoteCallGet("/api/v1/service-requests/{serviceRequestId}/offers/{offerId}/part-terms-preview")]
    Task<AizenApiResponse<Aizen.Modules.Payment.Abstraction.RemoteCall.Responses.ResolvePartLineAllowancesRemoteCallResponse>> GetOfferPartTermsPreview(
        long serviceRequestId, long offerId);

    // BE_WC3c — removed GetProviderConversations remote-call (SR /provider/conversations): dead after the read cutover
    // (provider chat list is served from Messaging via /provider/messaging/conversations).

    // --- Disputes (provider-scoped; cost-free) ---
    [AizenRemoteCallGet("/api/v1/service-requests/provider/disputes")]
    Task<AizenApiResponse<GetProviderDisputesResponse>> GetProviderDisputes(
        [Refit.Query] int pageIndex = 0,
        [Refit.Query] int pageSize = 20,
        [Refit.Query] ServiceRequestDisputeStatus? status = null);

    // --- Catalog ---
    [AizenRemoteCallGet("/api/v1/service-requests/provider/catalog")]
    Task<AizenApiResponse<List<Aizen.Modules.ServiceRequest.Abstraction.Dto.ProviderCatalogItemDto>>> ListCatalogItems();

    [AizenRemoteCallPost("/api/v1/service-requests/provider/catalog")]
    Task<AizenApiResponse<Aizen.Modules.ServiceRequest.Abstraction.Dto.ProviderCatalogItemDto>> CreateCatalogItem(
        [AizenRemoteCallBody] Aizen.Modules.ServiceRequest.Abstraction.Request.Offer.CatalogItemRequest body);

    [AizenRemoteCallPut("/api/v1/service-requests/provider/catalog/{id}")]
    Task<AizenApiResponse<Aizen.Modules.ServiceRequest.Abstraction.Dto.ProviderCatalogItemDto>> UpdateCatalogItem(
        long id, [AizenRemoteCallBody] Aizen.Modules.ServiceRequest.Abstraction.Request.Offer.CatalogItemRequest body);

    [AizenRemoteCallDelete("/api/v1/service-requests/provider/catalog/{id}")]
    Task DeleteCatalogItem(long id);

    // --- Templates ---
    [AizenRemoteCallGet("/api/v1/service-requests/provider/templates")]
    Task<AizenApiResponse<List<Aizen.Modules.ServiceRequest.Abstraction.Dto.ProviderOfferTemplateDto>>> ListTemplates();

    [AizenRemoteCallGet("/api/v1/service-requests/provider/templates/{id}")]
    Task<AizenApiResponse<Aizen.Modules.ServiceRequest.Abstraction.Dto.ProviderOfferTemplateDto>> GetTemplate(long id);

    [AizenRemoteCallPost("/api/v1/service-requests/provider/templates")]
    Task<AizenApiResponse<Aizen.Modules.ServiceRequest.Abstraction.Dto.ProviderOfferTemplateDto>> CreateTemplate(
        [AizenRemoteCallBody] Aizen.Modules.ServiceRequest.Abstraction.Request.Offer.OfferTemplateRequest body);

    [AizenRemoteCallPut("/api/v1/service-requests/provider/templates/{id}")]
    Task<AizenApiResponse<Aizen.Modules.ServiceRequest.Abstraction.Dto.ProviderOfferTemplateDto>> UpdateTemplate(
        long id, [AizenRemoteCallBody] Aizen.Modules.ServiceRequest.Abstraction.Request.Offer.OfferTemplateRequest body);

    [AizenRemoteCallDelete("/api/v1/service-requests/provider/templates/{id}")]
    Task DeleteTemplate(long id);

    [AizenRemoteCallGet("/api/v1/service-requests/provider/discovery/markers")]
    Task<AizenApiResponse<ProviderDiscoveryMarkersResponse>> GetDiscoveryMarkers(
        [Refit.Query] decimal? BoundsMinLat = null, [Refit.Query] decimal? BoundsMaxLat = null,
        [Refit.Query] decimal? BoundsMinLng = null, [Refit.Query] decimal? BoundsMaxLng = null,
        [Refit.Query] string? LocationCityCode = null, [Refit.Query] string? LocationCountryCode = null,
        [Refit.Query] string? ServiceCategoryCode = null, [Refit.Query] string? SearchTerm = null);

    [AizenRemoteCallGet("/api/v1/service-requests/provider/discovery/summary")]
    Task<AizenApiResponse<ProviderDiscoverySummaryResponse>> GetDiscoverySummary(
        [Refit.Query] string? LocationCityCode = null, [Refit.Query] string? LocationCountryCode = null,
        [Refit.Query] string? ServiceCategoryCode = null, [Refit.Query] string? SearchTerm = null,
        [Refit.Query] decimal? CenterLatitude = null, [Refit.Query] decimal? CenterLongitude = null,
        [Refit.Query] decimal? RadiusKm = null,
        [Refit.Query] decimal? BoundsMinLat = null, [Refit.Query] decimal? BoundsMaxLat = null,
        [Refit.Query] decimal? BoundsMinLng = null, [Refit.Query] decimal? BoundsMaxLng = null);

    [AizenRemoteCallGet("/api/v1/service-requests/provider/discovery")]
    Task<AizenApiResponse<ProviderDiscoveryResponse>> GetProviderDiscovery(
        [Refit.Query] int pageSize = 20,
        [Refit.Query] string? cursor = null,
        [Refit.Query] string? sortBy = null,
        [Refit.Query] string? locationCityCode = null,
        [Refit.Query] string? locationCountryCode = null,
        [Refit.Query] string? serviceCategoryCode = null,
        [Refit.Query] ServiceRequestPriority? minPriority = null,
        [Refit.Query] string? searchTerm = null,
        [Refit.Query] int? offerState = null,
        [Refit.Query] DateTime? publishedAfterUtc = null,
        [Refit.Query] decimal? centerLatitude = null,
        [Refit.Query] decimal? centerLongitude = null,
        [Refit.Query] decimal? radiusKm = null,
        [Refit.Query] decimal? boundsMinLat = null,
        [Refit.Query] decimal? boundsMaxLat = null,
        [Refit.Query] decimal? boundsMinLng = null,
        [Refit.Query] decimal? boundsMaxLng = null);
}
