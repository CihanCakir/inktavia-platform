using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ServiceRequest.Abstraction.Request.WorkLog;
using Aizen.Modules.ServiceRequest.Abstraction.Response.WorkLog;
using Aizen.Modules.ServiceRequest.Application.Command.WorkLog;
using Aizen.Modules.ServiceRequest.Application.Query.WorkLog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.ServiceRequest.Controller.V1.WorkLog;

[ApiController]
[Route("api/v1/service-requests/{serviceRequestId:long}/work-logs")]
[Tags("ServiceRequest - WorkLogs")]
[Authorize]
[DocumentationInfo("WorkLog endpoints", "Provider work log entries for service request assignments.")]
public sealed class ServiceRequestWorkLogController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public ServiceRequestWorkLogController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    [HttpPost("assignment/{assignmentId:long}")]
    [ProducesResponseType(typeof(AddServiceRequestWorkLogResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AddServiceRequestWorkLogResponse?>> Add(
        [FromRoute] long serviceRequestId, [FromRoute] long assignmentId,
        [FromBody] AddServiceRequestWorkLogRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<AddServiceRequestWorkLogResponse>(
            new AddServiceRequestWorkLogCommand(assignmentId, req), ct);
        return SetResponse(result);
    }

    [HttpGet("assignment/{assignmentId:long}")]
    [ProducesResponseType(typeof(GetServiceRequestWorkLogsResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetServiceRequestWorkLogsResponse?>> GetByAssignment(
        [FromRoute] long serviceRequestId, [FromRoute] long assignmentId, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<GetServiceRequestWorkLogsResponse>(
            new GetServiceRequestWorkLogsQuery(assignmentId), ct);
        return SetResponse(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(GetWorkLogsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<AizenApiResponse<GetWorkLogsResponse?>> GetWorkLogs(
        [FromRoute] long serviceRequestId, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<GetWorkLogsResponse>(
            new GetWorkLogsQuery(serviceRequestId), ct);
        return SetResponse(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(AddWorkLogEntryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<AizenApiResponse<AddWorkLogEntryResponse?>> AddEntry(
        [FromRoute] long serviceRequestId,
        [FromBody] AddWorkLogEntryRequest req,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<AddWorkLogEntryResponse>(
            new AddWorkLogEntryCommand(serviceRequestId, req), ct);
        return SetResponse(result);
    }

    [HttpPatch("phases/{phaseNumber:int}")]
    [ProducesResponseType(typeof(UpdateWorkPhaseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<AizenApiResponse<UpdateWorkPhaseResponse?>> UpdatePhase(
        [FromRoute] long serviceRequestId,
        [FromRoute] int phaseNumber,
        [FromBody] UpdateWorkPhaseRequest req,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<UpdateWorkPhaseResponse>(
            new UpdateWorkPhaseCommand(serviceRequestId, phaseNumber, req), ct);
        return SetResponse(result);
    }
}
