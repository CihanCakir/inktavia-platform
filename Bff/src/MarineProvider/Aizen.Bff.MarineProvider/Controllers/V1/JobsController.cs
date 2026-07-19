using Aizen.Bff.MarineProvider.Application.Common.Authorization;
using Aizen.Bff.MarineProvider.Application.Jobs;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Completion;
using Aizen.Modules.ServiceRequest.Abstraction.Request.WorkLog;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Jobs;
using Aizen.Modules.ServiceRequest.Abstraction.Response.WorkLog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GetProviderJobsResponse = Aizen.Bff.MarineProvider.Application.Contracts.Jobs.GetProviderJobsResponse;

namespace Aizen.Bff.MarineProvider.Controllers.V1;

/// <summary>
/// Provider workspace: assigned jobs. Requires an Approved + Active provider profile (runtime Identity status).
/// Provider identity is resolved server-side and asserted to the ServiceRequest module; the client cannot pass a
/// provider id.
/// </summary>
[ApiController]
[Route("api/v1/provider/jobs")]
[Tags("Provider - Jobs")]
[Authorize(Policy = ProviderAuthorizationPolicies.ProviderActive)]
public sealed class JobsController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public JobsController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor) => _cqrs = cqrs;

    /// <summary>List the caller provider's assigned jobs (paged), enriched with SR title + vessel name.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(GetProviderJobsResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetProviderJobsResponse>> GetJobs(
        [FromQuery] int pageIndex = 0, [FromQuery] int pageSize = 50, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new GetProviderJobsQuery(pageIndex, pageSize), ct));

    /// <summary>Job detail aggregate (assignment + SR + accepted offer + timeline).</summary>
    [HttpGet("{assignmentId:long}")]
    [ProducesResponseType(typeof(GetProviderJobDetailResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetProviderJobDetailResponse?>> GetJobDetail(
        [FromRoute] long assignmentId, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new GetJobDetailBffQuery { AssignmentId = assignmentId }, ct));

    /// <summary>List work logs for a job.</summary>
    [HttpGet("{assignmentId:long}/work-logs")]
    [ProducesResponseType(typeof(GetServiceRequestWorkLogsResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetServiceRequestWorkLogsResponse?>> GetWorkLogs(
        [FromRoute] long assignmentId, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new GetJobWorkLogsBffQuery { AssignmentId = assignmentId }, ct));

    /// <summary>Add a work log entry.</summary>
    [HttpPost("{assignmentId:long}/work-logs")]
    [ProducesResponseType(typeof(AddServiceRequestWorkLogResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AddServiceRequestWorkLogResponse?>> AddWorkLog(
        [FromRoute] long assignmentId, [FromBody] AddServiceRequestWorkLogRequest body, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new AddJobWorkLogBffCommand { AssignmentId = assignmentId, Body = body }, ct));

    /// <summary>Start the job (Assigned/Scheduled -> InProgress).</summary>
    [HttpPost("{assignmentId:long}/start")]
    [ProducesResponseType(typeof(JobSuccessResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<JobSuccessResult>> StartJob(
        [FromRoute] long assignmentId, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new StartJobBffCommand { AssignmentId = assignmentId }, ct));

    /// <summary>Submit completion (InProgress -> CompletionSubmitted).</summary>
    [HttpPost("{assignmentId:long}/complete")]
    [ProducesResponseType(typeof(JobSuccessResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<JobSuccessResult>> CompleteJob(
        [FromRoute] long assignmentId, [FromBody] SubmitServiceRequestCompletionRequest body, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new CompleteJobBffCommand { AssignmentId = assignmentId, Body = body }, ct));

    /// <summary>Global status counts for the KPI board + donut (not paged).</summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(GetProviderJobsSummaryResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetProviderJobsSummaryResponse?>> GetJobsSummary(CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new GetJobsSummaryBffQuery(), ct));

    /// <summary>Weekly workload buckets for the stacked bar chart.</summary>
    [HttpGet("workload")]
    [ProducesResponseType(typeof(GetProviderJobsWorkloadResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetProviderJobsWorkloadResponse?>> GetJobsWorkload(
        [FromQuery] int weeks = 6, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new GetJobsWorkloadBffQuery { Weeks = weeks }, ct));

    /// <summary>Jobs needing intervention, grouped (ownerApproval, materialRequired, blocked).</summary>
    [HttpGet("action-required")]
    [ProducesResponseType(typeof(GetProviderJobsActionRequiredResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetProviderJobsActionRequiredResponse?>> GetJobsActionRequired(CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new GetJobsActionRequiredBffQuery(), ct));
}
