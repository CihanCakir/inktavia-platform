using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Completion;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Completion;
using Aizen.Modules.ServiceRequest.Application.Command.Completion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.ServiceRequest.Controller.V1.Completion;

[ApiController]
[Route("api/v1/service-requests/{serviceRequestId:long}/completion")]
[Tags("ServiceRequest - Completion")]
[Authorize]
[DocumentationInfo("Completion endpoints", "Completion submission, approval and rejection for service requests.")]
public sealed class ServiceRequestCompletionController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public ServiceRequestCompletionController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    [HttpPost("{assignmentId:long}")]
    [ProducesResponseType(typeof(SubmitServiceRequestCompletionResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<SubmitServiceRequestCompletionResponse?>> Submit(
        [FromRoute] long serviceRequestId, [FromRoute] long assignmentId,
        [FromBody] SubmitServiceRequestCompletionRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<SubmitServiceRequestCompletionResponse>(
            new SubmitServiceRequestCompletionCommand(assignmentId, req), ct);
        return SetResponse(result);
    }

    [HttpPatch("approve")]
    [ProducesResponseType(typeof(ApproveServiceRequestCompletionResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ApproveServiceRequestCompletionResponse?>> Approve(
        [FromRoute] long serviceRequestId, [FromBody] ApproveServiceRequestCompletionRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<ApproveServiceRequestCompletionResponse>(
            new ApproveServiceRequestCompletionCommand(serviceRequestId, req), ct);
        return SetResponse(result);
    }

    [HttpPatch("reject")]
    [ProducesResponseType(typeof(RejectServiceRequestCompletionResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<RejectServiceRequestCompletionResponse?>> Reject(
        [FromRoute] long serviceRequestId, [FromBody] RejectServiceRequestCompletionRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<RejectServiceRequestCompletionResponse>(
            new RejectServiceRequestCompletionCommand(serviceRequestId, req), ct);
        return SetResponse(result);
    }
}
