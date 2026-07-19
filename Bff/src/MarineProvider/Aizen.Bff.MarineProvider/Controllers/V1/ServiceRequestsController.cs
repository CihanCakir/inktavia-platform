using Aizen.Bff.MarineProvider.Application.Common.Authorization;
using Aizen.Bff.MarineProvider.Application.ServiceRequests;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Provider;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.MarineProvider.Controllers.V1;

[ApiController]
[Route("api/v1/provider/service-requests")]
[Tags("Provider - Service Requests")]
[Authorize(Policy = ProviderAuthorizationPolicies.ProviderActive)]
public sealed class ServiceRequestsController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public ServiceRequestsController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor) => _cqrs = cqrs;

    [HttpGet("open")]
    [ProducesResponseType(typeof(GetOpenServiceRequestsResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetOpenServiceRequestsResponse?>> GetOpen(
        [FromQuery] int pageIndex = 0, [FromQuery] int pageSize = 20,
        [FromQuery] string? serviceCategoryCode = null, [FromQuery] string? locationCityCode = null,
        [FromQuery] string? locationCountryCode = null, [FromQuery] string? searchTerm = null,
        CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new GetOpenServiceRequestsBffQuery
        {
            PageIndex = pageIndex, PageSize = pageSize,
            ServiceCategoryCode = serviceCategoryCode, LocationCityCode = locationCityCode,
            LocationCountryCode = locationCountryCode, SearchTerm = searchTerm,
        }, ct));

    /// <summary>
    /// One request. The module rejects it when the calling provider has no relationship with it — a provider must
    /// not be able to read an arbitrary request by guessing an id.
    /// </summary>
    [HttpGet("{serviceRequestId:long}")]
    [ProducesResponseType(typeof(GetProviderServiceRequestDetailResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetProviderServiceRequestDetailResponse?>> GetDetail(
        [FromRoute] long serviceRequestId,
        [FromQuery] decimal? centerLatitude = null, [FromQuery] decimal? centerLongitude = null,
        CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new GetServiceRequestDetailBffQuery
        {
            ServiceRequestId = serviceRequestId,
            CenterLatitude = centerLatitude, CenterLongitude = centerLongitude,
        }, ct));

    /// <summary>
    /// Map + list discovery — the paged, vessel-enriched list. This is the primary discovery endpoint; markers
    /// and summary are its siblings.
    /// </summary>
    [HttpGet("discovery")]
    [ProducesResponseType(typeof(GetProviderDiscoveryBffResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetProviderDiscoveryBffResponse?>> GetDiscovery(
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
        => SetResponse(await _cqrs.ProcessAsync(new GetProviderDiscoveryBffQuery
        {
            PageSize = pageSize, Cursor = cursor, SortBy = sort,
            LocationCityCode = locationCityCode, LocationCountryCode = locationCountryCode,
            ServiceCategoryCode = serviceCategoryCode, MinPriority = minPriority,
            SearchTerm = searchTerm, OfferState = offerState,
            PublishedAfterUtc = newSince,
            CenterLatitude = centerLatitude, CenterLongitude = centerLongitude, RadiusKm = radiusKm,
            BoundsMinLat = boundsMinLat, BoundsMaxLat = boundsMaxLat,
            BoundsMinLng = boundsMinLng, BoundsMaxLng = boundsMaxLng,
        }, ct));

    [HttpGet("discovery/markers")]
    [ProducesResponseType(typeof(ProviderDiscoveryMarkersResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProviderDiscoveryMarkersResponse?>> GetDiscoveryMarkers(
        [FromQuery] decimal? boundsMinLat, [FromQuery] decimal? boundsMaxLat,
        [FromQuery] decimal? boundsMinLng, [FromQuery] decimal? boundsMaxLng,
        [FromQuery] string? locationCityCode, [FromQuery] string? locationCountryCode,
        [FromQuery] string? serviceCategoryCode, [FromQuery] string? searchTerm,
        CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new GetDiscoveryMarkersBffQuery
        {
            BoundsMinLat = boundsMinLat, BoundsMaxLat = boundsMaxLat,
            BoundsMinLng = boundsMinLng, BoundsMaxLng = boundsMaxLng,
            LocationCityCode = locationCityCode, LocationCountryCode = locationCountryCode,
            ServiceCategoryCode = serviceCategoryCode, SearchTerm = searchTerm,
        }, ct));

    [HttpGet("discovery/summary")]
    [ProducesResponseType(typeof(ProviderDiscoverySummaryResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProviderDiscoverySummaryResponse?>> GetDiscoverySummary(
        [FromQuery] string? locationCityCode, [FromQuery] string? locationCountryCode,
        [FromQuery] string? serviceCategoryCode, [FromQuery] string? searchTerm,
        [FromQuery] decimal? centerLatitude, [FromQuery] decimal? centerLongitude,
        [FromQuery] decimal? radiusKm,
        [FromQuery] decimal? boundsMinLat, [FromQuery] decimal? boundsMaxLat,
        [FromQuery] decimal? boundsMinLng, [FromQuery] decimal? boundsMaxLng,
        CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new GetDiscoverySummaryBffQuery
        {
            LocationCityCode = locationCityCode, LocationCountryCode = locationCountryCode,
            ServiceCategoryCode = serviceCategoryCode, SearchTerm = searchTerm,
            CenterLatitude = centerLatitude, CenterLongitude = centerLongitude, RadiusKm = radiusKm,
            BoundsMinLat = boundsMinLat, BoundsMaxLat = boundsMaxLat,
            BoundsMinLng = boundsMinLng, BoundsMaxLng = boundsMaxLng,
        }, ct));

    /// <summary>
    /// Mints a short-lived signed read URL for a service request attachment.
    /// Access-scoped: the module verifies the provider's relationship to the request and that the
    /// fileId is actually an attachment on it.
    /// </summary>
    [HttpGet("{serviceRequestId:long}/attachments/{fileId:guid}/read-url")]
    [ProducesResponseType(typeof(AttachmentReadUrlBffResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AttachmentReadUrlBffResponse?>> GetAttachmentReadUrl(
        [FromRoute] long serviceRequestId, [FromRoute] Guid fileId, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new GetAttachmentReadUrlBffQuery
        {
            ServiceRequestId = serviceRequestId, FileId = fileId,
        }, ct));

    /// <summary>Conversation history + channelOpen state. Access-checked via assertion.</summary>
    [HttpGet("{serviceRequestId:long}/messages")]
    [ProducesResponseType(typeof(ProviderMessagesResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProviderMessagesResponse?>> GetMessages(
        [FromRoute] long serviceRequestId, [FromQuery] int skip = 0, [FromQuery] int take = 50,
        CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new GetProviderMessagesQuery
        {
            ServiceRequestId = serviceRequestId, Skip = skip, Take = take,
        }, ct));

    /// <summary>
    /// Provider sends a free-text message. The module enforces the anti-harassment gate
    /// (SR_MSG_CHANNEL_LOCKED when customer hasn't replied yet).
    /// </summary>
    [HttpPost("{serviceRequestId:long}/messages")]
    [ProducesResponseType(typeof(SendServiceRequestMessageResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<SendServiceRequestMessageResponse?>> SendMessage(
        [FromRoute] long serviceRequestId, [FromBody] SendProviderMessageRequest body,
        CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new SendProviderMessageCommand
        {
            ServiceRequestId = serviceRequestId,
            Content = body.Content,
            AttachmentFileId = body.AttachmentFileId,
            LocationLat = body.LocationLat,
            LocationLng = body.LocationLng,
            LocationLabel = body.LocationLabel,
        }, ct));

    /// <summary>Provider's conversation list (inbox). Provider-scoped.</summary>
    [HttpGet("conversations")]
    [ProducesResponseType(typeof(GetProviderConversationsResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetProviderConversationsResponse?>> GetConversations(CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new GetProviderConversationsBffQuery(), ct));
}
