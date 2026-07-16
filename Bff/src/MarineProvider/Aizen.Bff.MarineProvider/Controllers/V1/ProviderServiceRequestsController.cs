using Aizen.Bff.MarineProvider.Application.Common.Authorization;
using Aizen.Bff.MarineProvider.Application.ServiceRequests;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.MarineProvider.Controllers.V1;

[ApiController]
[Route("api/v1/provider/service-requests")]
[Tags("Provider - Service Requests")]
[Authorize(Policy = ProviderAuthorizationPolicies.ProviderActive)]
public sealed class ProviderServiceRequestsController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public ProviderServiceRequestsController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    [HttpGet("open")]
    public async Task<IActionResult> GetOpen(
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? serviceCategoryCode = null,
        [FromQuery] string? locationCityCode = null,
        [FromQuery] string? locationCountryCode = null,
        [FromQuery] string? searchTerm = null,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new GetOpenServiceRequestsBffQuery
        {
            PageIndex = pageIndex,
            PageSize = pageSize,
            ServiceCategoryCode = serviceCategoryCode,
            LocationCityCode = locationCityCode,
            LocationCountryCode = locationCountryCode,
            SearchTerm = searchTerm,
        }, ct);
        return Ok(SetResponse(result));
    }

    /// <summary>
    /// One request. The module rejects it when the calling provider has no relationship with it — a provider must
    /// not be able to read an arbitrary request by guessing an id.
    /// </summary>
    [HttpGet("{serviceRequestId:long}")]
    public async Task<IActionResult> GetDetail([FromRoute] long serviceRequestId, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new GetServiceRequestDetailBffQuery { ServiceRequestId = serviceRequestId }, ct);

        return Ok(SetResponse(result));
    }

    /// <summary>
    /// Map + list discovery — the paged, vessel-enriched list. This is the primary discovery endpoint; markers
    /// and summary are its siblings. The route is a literal ("discovery"), so it never collides with the
    /// numeric detail route above ({serviceRequestId:long}).
    /// </summary>
    [HttpGet("discovery")]
    public async Task<IActionResult> GetDiscovery(
        [FromQuery] int pageSize = 20, [FromQuery] string? cursor = null, [FromQuery] string? sort = null,
        [FromQuery] string? locationCityCode = null, [FromQuery] string? locationCountryCode = null,
        [FromQuery] string? serviceCategoryCode = null, [FromQuery] ServiceRequestPriority? minPriority = null,
        [FromQuery] string? searchTerm = null, [FromQuery] string? offerState = null,
        [FromQuery] DateTime? newSince = null,
        [FromQuery] decimal? centerLatitude = null, [FromQuery] decimal? centerLongitude = null,
        [FromQuery] decimal? radiusKm = null,
        [FromQuery] decimal? boundsMinLat = null, [FromQuery] decimal? boundsMaxLat = null,
        [FromQuery] decimal? boundsMinLng = null, [FromQuery] decimal? boundsMaxLng = null,
        CancellationToken ct = default)
    {
        // OfferState arrives as the enum NAME ("NotOffered") to stay readable on the wire; map to the int the
        // BFF query carries (Any=0, NotOffered=1, Offered=2). An unknown value falls through to Any.
        int? offerStateCode = offerState?.Trim().ToLowerInvariant() switch
        {
            "notoffered" => 1,
            "offered" => 2,
            _ => null,
        };

        var result = await _cqrs.ProcessAsync(new GetProviderDiscoveryBffQuery
        {
            PageSize = pageSize,
            Cursor = cursor,
            SortBy = sort,
            LocationCityCode = locationCityCode,
            LocationCountryCode = locationCountryCode,
            ServiceCategoryCode = serviceCategoryCode,
            MinPriority = minPriority,
            SearchTerm = searchTerm,
            OfferState = offerStateCode,
            PublishedAfterUtc = newSince,
            CenterLatitude = centerLatitude,
            CenterLongitude = centerLongitude,
            RadiusKm = radiusKm,
            BoundsMinLat = boundsMinLat, BoundsMaxLat = boundsMaxLat,
            BoundsMinLng = boundsMinLng, BoundsMaxLng = boundsMaxLng,
        }, ct);
        return Ok(SetResponse(result));
    }

    [HttpGet("discovery/markers")]
    public async Task<IActionResult> GetDiscoveryMarkers(
        [FromQuery] decimal? boundsMinLat, [FromQuery] decimal? boundsMaxLat,
        [FromQuery] decimal? boundsMinLng, [FromQuery] decimal? boundsMaxLng,
        [FromQuery] string? locationCityCode, [FromQuery] string? locationCountryCode,
        [FromQuery] string? serviceCategoryCode, [FromQuery] string? searchTerm,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new GetDiscoveryMarkersBffQuery
        {
            BoundsMinLat = boundsMinLat, BoundsMaxLat = boundsMaxLat,
            BoundsMinLng = boundsMinLng, BoundsMaxLng = boundsMaxLng,
            LocationCityCode = locationCityCode, LocationCountryCode = locationCountryCode,
            ServiceCategoryCode = serviceCategoryCode, SearchTerm = searchTerm,
        }, ct);
        return Ok(SetResponse(result));
    }

    [HttpGet("discovery/summary")]
    public async Task<IActionResult> GetDiscoverySummary(
        [FromQuery] string? locationCityCode, [FromQuery] string? locationCountryCode,
        [FromQuery] string? serviceCategoryCode, [FromQuery] string? searchTerm,
        [FromQuery] decimal? centerLatitude, [FromQuery] decimal? centerLongitude,
        [FromQuery] decimal? radiusKm,
        [FromQuery] decimal? boundsMinLat, [FromQuery] decimal? boundsMaxLat,
        [FromQuery] decimal? boundsMinLng, [FromQuery] decimal? boundsMaxLng,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new GetDiscoverySummaryBffQuery
        {
            LocationCityCode = locationCityCode, LocationCountryCode = locationCountryCode,
            ServiceCategoryCode = serviceCategoryCode, SearchTerm = searchTerm,
            CenterLatitude = centerLatitude, CenterLongitude = centerLongitude,
            RadiusKm = radiusKm,
            BoundsMinLat = boundsMinLat, BoundsMaxLat = boundsMaxLat,
            BoundsMinLng = boundsMinLng, BoundsMaxLng = boundsMaxLng,
        }, ct);
        return Ok(SetResponse(result));
    }

    /// <summary>
    /// Mints a short-lived signed read URL for a service request attachment.
    /// Access-scoped: the module verifies the provider's relationship to the request and that the
    /// fileId is actually an attachment on it. No object key or bucket is returned.
    /// </summary>
    [HttpGet("{serviceRequestId:long}/attachments/{fileId:guid}/read-url")]
    public async Task<IActionResult> GetAttachmentReadUrl(
        [FromRoute] long serviceRequestId, [FromRoute] Guid fileId,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new GetAttachmentReadUrlBffQuery
        {
            ServiceRequestId = serviceRequestId,
            FileId = fileId,
        }, ct);
        return Ok(SetResponse(result));
    }
}
