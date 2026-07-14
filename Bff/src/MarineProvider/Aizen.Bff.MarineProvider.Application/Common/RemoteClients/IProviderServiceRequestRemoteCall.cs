using Aizen.Core.Infrastructure.Api;
using Aizen.Core.RemoteCall.Abstraction;
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
    Task<AizenApiResponse<GetServiceRequestDetailResponse>> GetServiceRequestDetail(long serviceRequestId);

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
}
