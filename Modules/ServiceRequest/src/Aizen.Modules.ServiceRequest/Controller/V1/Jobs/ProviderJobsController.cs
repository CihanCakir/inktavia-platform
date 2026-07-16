using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Filter;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Jobs;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Provider;
using Aizen.Modules.ServiceRequest.Application.Query.Jobs;
using Aizen.Modules.ServiceRequest.Application.Query.Provider.GetMyOffers;
using Aizen.Modules.ServiceRequest.Application.Query.Provider.GetOpenServiceRequests;
using Aizen.Modules.ServiceRequest.Application.Query.Provider.GetProviderDiscovery;
using Aizen.Modules.ServiceRequest.Application.Query.Provider.GetDiscoveryMarkers;
using Aizen.Modules.ServiceRequest.Application.Query.Provider.GetDiscoverySummary;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;
using Aizen.Modules.ServiceRequest.Application.Query.Provider.GetProviderServiceRequestDetail;
using Aizen.Modules.ServiceRequest.Application.Query.Provider.GetAttachmentAccessUrl;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.ServiceRequest.Controller.V1.Jobs;

/// <summary>
/// Provider-scoped reads: jobs, open service requests, and offers. Provider identity is taken from the
/// trusted request context (BFF assertion), never from route/query.
/// </summary>
[ApiController]
[Route("api/v1/service-requests/provider")]
[Tags("ServiceRequest - Provider")]
[Authorize]
public sealed class ProviderJobsController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public ProviderJobsController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    [HttpGet("jobs")]
    [ProducesResponseType(typeof(GetProviderJobsResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetProviderJobsResponse?>> GetJobs(
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<GetProviderJobsResponse>(
            new GetProviderJobsQuery(pageIndex, pageSize), ct);

        return SetResponse(result);
    }

    [HttpGet("open")]
    [ProducesResponseType(typeof(GetOpenServiceRequestsResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetOpenServiceRequestsResponse?>> GetOpenServiceRequests(
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? serviceCategoryCode = null,
        [FromQuery] string? locationCityCode = null,
        [FromQuery] string? locationCountryCode = null,
        [FromQuery] string? searchTerm = null,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<GetOpenServiceRequestsResponse>(
            new GetOpenServiceRequestsQuery(pageIndex, pageSize)
            {
                ServiceCategoryCode = serviceCategoryCode,
                LocationCityCode = locationCityCode,
                LocationCountryCode = locationCountryCode,
                SearchTerm = searchTerm,
            }, ct);

        return SetResponse(result);
    }

    [HttpGet("my-offers")]
    [ProducesResponseType(typeof(GetMyOffersResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetMyOffersResponse?>> GetMyOffers(
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        [FromQuery] ServiceRequestOfferStatus? status = null,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<GetMyOffersResponse>(
            new GetMyOffersQuery(pageIndex, pageSize) { StatusFilter = status }, ct);

        return SetResponse(result);
    }

    [HttpGet("discovery")]
    [ProducesResponseType(typeof(ProviderDiscoveryResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProviderDiscoveryResponse?>> GetDiscovery(
        [FromQuery] ProviderServiceRequestDiscoveryFilter filter,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<ProviderDiscoveryResponse>(
            new GetProviderDiscoveryQuery { Filter = filter }, ct);

        return SetResponse(result);
    }

    [HttpGet("discovery/markers")]
    [ProducesResponseType(typeof(ProviderDiscoveryMarkersResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProviderDiscoveryMarkersResponse?>> GetDiscoveryMarkers(
        [FromQuery] ProviderServiceRequestDiscoveryFilter filter,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<ProviderDiscoveryMarkersResponse>(
            new GetDiscoveryMarkersQuery { Filter = filter }, ct);

        return SetResponse(result);
    }

    [HttpGet("discovery/summary")]
    [ProducesResponseType(typeof(ProviderDiscoverySummaryResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProviderDiscoverySummaryResponse?>> GetDiscoverySummary(
        [FromQuery] ProviderServiceRequestDiscoveryFilter filter,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<ProviderDiscoverySummaryResponse>(
            new GetDiscoverySummaryQuery { Filter = filter }, ct);

        return SetResponse(result);
    }

    /// <summary>
    /// One service request, readable only when the calling provider has a relationship with it (it is biddable,
    /// they have an offer on it, or it is assigned to them). Without this a provider could walk the id space and
    /// read every customer's request in the system.
    /// </summary>
    [HttpGet("service-requests/{serviceRequestId:long}")]
    [ProducesResponseType(typeof(GetProviderServiceRequestDetailResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetProviderServiceRequestDetailResponse?>> GetServiceRequestDetail(
        [FromRoute] long serviceRequestId,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<GetProviderServiceRequestDetailResponse>(
            new GetProviderServiceRequestDetailQuery(serviceRequestId), ct);

        return SetResponse(result);
    }

    /// <summary>
    /// Access-checks an attachment on a request for the calling provider.
    /// Returns the FileId if authorized — the BFF mints the signed URL.
    /// </summary>
    [HttpGet("service-requests/{serviceRequestId:long}/attachments/{fileId:guid}/access-check")]
    [ProducesResponseType(typeof(GetAttachmentAccessCheckResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetAttachmentAccessCheckResponse?>> CheckAttachmentAccess(
        [FromRoute] long serviceRequestId, [FromRoute] Guid fileId,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<GetAttachmentAccessCheckResponse>(
            new GetAttachmentAccessCheckQuery(serviceRequestId, fileId), ct);
        return SetResponse(result);
    }
}
