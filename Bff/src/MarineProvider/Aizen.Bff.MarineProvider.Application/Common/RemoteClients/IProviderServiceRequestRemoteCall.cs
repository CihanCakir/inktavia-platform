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
public interface IProviderServiceRequestRemoteCall : IAizenRemoteCall
{
    [AizenRemoteCallGet("/api/v1/service-requests/provider/jobs")]
    Task<AizenApiResponse<GetProviderJobsResponse>> GetProviderJobs(
        [Refit.Query] int pageIndex = 0,
        [Refit.Query] int pageSize = 50);

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
    Task<AizenApiResponse<GetProviderServiceRequestDetailResponse>> GetServiceRequestDetail(long serviceRequestId);

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
