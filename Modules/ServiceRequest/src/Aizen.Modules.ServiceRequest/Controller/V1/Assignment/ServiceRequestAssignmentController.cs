using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ServiceRequest.Abstraction.Model;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Assignment;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Assignment;
using Aizen.Modules.ServiceRequest.Application.Command.Assignment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.ServiceRequest.Controller.V1.Assignment;

[ApiController]
[Route("api/v1/service-requests/{serviceRequestId:long}/assignment")]
[Tags("ServiceRequest - Assignment")]
[Authorize]
[DocumentationInfo("Assignment endpoints", "Service request assignment lifecycle management.")]
public sealed class ServiceRequestAssignmentController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public ServiceRequestAssignmentController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateServiceRequestAssignmentResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CreateServiceRequestAssignmentResponse?>> Create(
        [FromRoute] long serviceRequestId, [FromBody] CreateServiceRequestAssignmentRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<CreateServiceRequestAssignmentResponse>(new CreateServiceRequestAssignmentCommand(serviceRequestId, req), ct);
        return SetResponse(result);
    }

    [HttpPatch("{assignmentId:long}/accept")]
    [ProducesResponseType(typeof(AcceptServiceRequestAssignmentResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AcceptServiceRequestAssignmentResponse?>> Accept(
        [FromRoute] long serviceRequestId, [FromRoute] long assignmentId, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<AcceptServiceRequestAssignmentResponse>(new AcceptServiceRequestAssignmentCommand(assignmentId), ct);
        return SetResponse(result);
    }

    [HttpPatch("{assignmentId:long}/reject")]
    [ProducesResponseType(typeof(RejectServiceRequestAssignmentResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<RejectServiceRequestAssignmentResponse?>> Reject(
        [FromRoute] long serviceRequestId, [FromRoute] long assignmentId, [FromBody] RejectServiceRequestAssignmentRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<RejectServiceRequestAssignmentResponse>(new RejectServiceRequestAssignmentCommand(assignmentId, req), ct);
        return SetResponse(result);
    }

    [HttpPatch("{assignmentId:long}/start")]
    [ProducesResponseType(typeof(StartServiceRequestAssignmentResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<StartServiceRequestAssignmentResponse?>> Start(
        [FromRoute] long serviceRequestId, [FromRoute] long assignmentId, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<StartServiceRequestAssignmentResponse>(new StartServiceRequestAssignmentCommand(assignmentId), ct);
        return SetResponse(result);
    }
}
