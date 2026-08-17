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
using Aizen.Modules.ServiceRequest.Application.Query.Provider.GetProviderDisputes;
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

    [HttpGet("jobs/summary")]
    [ProducesResponseType(typeof(GetProviderJobsSummaryResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetProviderJobsSummaryResponse?>> GetJobsSummary(CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<GetProviderJobsSummaryResponse>(
            new GetProviderJobsSummaryQuery(), ct);
        return SetResponse(result);
    }

    [HttpGet("jobs/workload")]
    [ProducesResponseType(typeof(GetProviderJobsWorkloadResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetProviderJobsWorkloadResponse?>> GetJobsWorkload(
        [FromQuery] int weeks = 6, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<GetProviderJobsWorkloadResponse>(
            new GetProviderJobsWorkloadQuery(weeks), ct);
        return SetResponse(result);
    }

    [HttpGet("jobs/action-required")]
    [ProducesResponseType(typeof(GetProviderJobsActionRequiredResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetProviderJobsActionRequiredResponse?>> GetJobsActionRequired(CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<GetProviderJobsActionRequiredResponse>(
            new GetProviderJobsActionRequiredQuery(), ct);
        return SetResponse(result);
    }

    [HttpGet("jobs/{assignmentId:long}")]
    [ProducesResponseType(typeof(GetProviderJobDetailResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetProviderJobDetailResponse?>> GetJobDetail(
        [FromRoute] long assignmentId, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<GetProviderJobDetailResponse>(
            new GetProviderJobDetailQuery(assignmentId), ct);
        return SetResponse(result);
    }

    [HttpPost("jobs/{assignmentId:long}/start")]
    public async Task<IActionResult> StartJob([FromRoute] long assignmentId, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<Abstraction.Response.Assignment.StartServiceRequestAssignmentResponse>(
            new Application.Command.Assignment.StartServiceRequestAssignmentCommand(assignmentId), ct);
        return Ok(new { Header = new { IsSuccess = true }, Body = new { AssignmentId = result?.AssignmentId } });
    }

    [HttpPost("jobs/{assignmentId:long}/complete")]
    public async Task<IActionResult> CompleteJob(
        [FromRoute] long assignmentId,
        [FromBody] Abstraction.Request.Completion.SubmitServiceRequestCompletionRequest req,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<Abstraction.Response.Completion.SubmitServiceRequestCompletionResponse>(
            new Application.Command.Completion.SubmitServiceRequestCompletionCommand(assignmentId, req), ct);
        return Ok(new { Header = new { IsSuccess = true }, Body = result });
    }

    // N-E — provider rejects an assigned job with a structured reason (+ optional note). Provider identity is taken
    // from the trusted request context; the reject command resolves the parent SR from the assignment itself.
    [HttpPost("jobs/{assignmentId:long}/reject")]
    public async Task<IActionResult> RejectJob(
        [FromRoute] long assignmentId,
        [FromBody] Abstraction.Request.Assignment.RejectServiceRequestAssignmentRequest req,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<Abstraction.Response.Assignment.RejectServiceRequestAssignmentResponse>(
            new Application.Command.Assignment.RejectServiceRequestAssignmentCommand(assignmentId, req), ct);
        return Ok(new { Header = new { IsSuccess = true }, Body = new { AssignmentId = result?.AssignmentId } });
    }

    [HttpGet("jobs/{assignmentId:long}/work-logs")]
    public async Task<AizenApiResponse<Abstraction.Response.WorkLog.GetServiceRequestWorkLogsResponse?>> GetWorkLogs(
        [FromRoute] long assignmentId, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<Abstraction.Response.WorkLog.GetServiceRequestWorkLogsResponse>(
            new Application.Query.WorkLog.GetServiceRequestWorkLogsQuery(assignmentId), ct);
        return SetResponse(result);
    }

    [HttpPost("jobs/{assignmentId:long}/work-logs")]
    public async Task<AizenApiResponse<Abstraction.Response.WorkLog.AddServiceRequestWorkLogResponse?>> AddWorkLog(
        [FromRoute] long assignmentId,
        [FromBody] Abstraction.Request.WorkLog.AddServiceRequestWorkLogRequest req,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<Abstraction.Response.WorkLog.AddServiceRequestWorkLogResponse>(
            new Application.Command.WorkLog.AddServiceRequestWorkLogCommand(assignmentId, req), ct);
        return SetResponse(result);
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
        [FromQuery] decimal? centerLatitude = null,
        [FromQuery] decimal? centerLongitude = null,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<GetProviderServiceRequestDetailResponse>(
            new GetProviderServiceRequestDetailQuery(serviceRequestId, centerLatitude, centerLongitude), ct);

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

    // BE_WC3c — removed the legacy SR-backed provider conversations read (GET .../provider/conversations). Provider
    // chat lists now read from the Messaging store (BFF /provider/messaging/conversations). No caller remained.

    /// <summary>
    /// The calling provider's own disputes — the disputes on the service requests they won — plus a global
    /// open/actionable count for the dashboard attention row. Provider identity is taken from the trusted context,
    /// never from parameters. Cost-free: no offer economics leave here.
    /// </summary>
    [HttpGet("disputes")]
    [ProducesResponseType(typeof(GetProviderDisputesResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetProviderDisputesResponse?>> GetDisputes(
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        [FromQuery] ServiceRequestDisputeStatus? status = null,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<GetProviderDisputesResponse>(
            new GetProviderDisputesQuery(pageIndex, pageSize) { StatusFilter = status }, ct);
        return SetResponse(result);
    }
}
