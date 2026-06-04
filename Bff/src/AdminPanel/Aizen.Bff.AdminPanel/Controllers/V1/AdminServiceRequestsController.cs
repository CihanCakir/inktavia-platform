using Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Command;
using Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Dto;
using Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Query;
using Aizen.Bff.AdminPanel.Application.Common.Dto;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Completion;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Dispute;
using Aizen.Modules.ServiceRequest.Abstraction.Request.ServiceRequest;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Admin;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Completion;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Dispute;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.AdminPanel.Controllers.V1;

[ApiController]
[Route("api/v1/admin-panel")]
[Tags("Admin Panel - Service Requests")]
[Authorize(Roles = "Admin")]
public sealed class ServiceRequestsController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public ServiceRequestsController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrsProcessor)
        : base(httpContextAccessor)
    {
        _cqrs = cqrsProcessor;
    }

    [HttpGet("service-requests")]
    [ProducesResponseType(typeof(AdminServiceRequestListResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AdminServiceRequestListResponse>> GetServiceRequests(
        [FromQuery] string? status,
        [FromQuery] long? vesselId,
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var auth = HttpContext.Request.Headers["Authorization"].FirstOrDefault() ?? string.Empty;
        var userToken = HttpContext.Request.Headers["X-Aizen-User-Token"].FirstOrDefault() ?? string.Empty;
        var result = await _cqrs.ProcessAsync(
            new GetAdminServiceRequestListQuery(auth, userToken, status, vesselId, pageIndex, pageSize), ct);
        return SetResponse(result);
    }

    [HttpGet("service-requests/disputes")]
    [ProducesResponseType(typeof(GetAdminDisputeListResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetAdminDisputeListResponse>> GetDisputes(
        [FromQuery] string? status,
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var auth = HttpContext.Request.Headers["Authorization"].FirstOrDefault() ?? string.Empty;
        var userToken = HttpContext.Request.Headers["X-Aizen-User-Token"].FirstOrDefault() ?? string.Empty;
        var result = await _cqrs.ProcessAsync(
            new GetAdminDisputeListQuery(auth, userToken, status, pageIndex, pageSize), ct);
        return SetResponse(result);
    }

    [HttpGet("service-requests/{serviceRequestId:long}")]
    [ProducesResponseType(typeof(AdminServiceRequestOperationDetailResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AdminServiceRequestOperationDetailResponse>> GetServiceRequestDetail(
        long serviceRequestId, CancellationToken ct)
    {
        var auth = HttpContext.Request.Headers["Authorization"].FirstOrDefault() ?? string.Empty;
        var userToken = HttpContext.Request.Headers["X-Aizen-User-Token"].FirstOrDefault() ?? string.Empty;
        var result = await _cqrs.ProcessAsync(
            new GetAdminServiceRequestOperationDetailQuery(serviceRequestId, auth, userToken), ct);
        return SetResponse(result);
    }

    [HttpPatch("service-requests/{serviceRequestId:long}/cancel")]
    [ProducesResponseType(typeof(CancelServiceRequestResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CancelServiceRequestResponse>> CancelServiceRequest(
        long serviceRequestId, [FromBody] CancelServiceRequestRequest request, CancellationToken ct)
    {
        var auth = HttpContext.Request.Headers["Authorization"].FirstOrDefault() ?? string.Empty;
        var userToken = HttpContext.Request.Headers["X-Aizen-User-Token"].FirstOrDefault() ?? string.Empty;
        var result = await _cqrs.ProcessAsync(
            new CancelServiceRequestCommand(serviceRequestId, request, auth, userToken), ct);
        return SetResponse(result);
    }

    [HttpPatch("service-requests/{serviceRequestId:long}/completion/approve")]
    [ProducesResponseType(typeof(ApproveServiceRequestCompletionResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ApproveServiceRequestCompletionResponse>> ApproveCompletion(
        long serviceRequestId, [FromBody] ApproveServiceRequestCompletionRequest request, CancellationToken ct)
    {
        var auth = HttpContext.Request.Headers["Authorization"].FirstOrDefault() ?? string.Empty;
        var userToken = HttpContext.Request.Headers["X-Aizen-User-Token"].FirstOrDefault() ?? string.Empty;
        var result = await _cqrs.ProcessAsync(
            new ApproveCompletionCommand(serviceRequestId, request, auth, userToken), ct);
        return SetResponse(result);
    }

    [HttpPatch("service-requests/{serviceRequestId:long}/completion/reject")]
    [ProducesResponseType(typeof(RejectServiceRequestCompletionResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<RejectServiceRequestCompletionResponse>> RejectCompletion(
        long serviceRequestId, [FromBody] RejectServiceRequestCompletionRequest request, CancellationToken ct)
    {
        var auth = HttpContext.Request.Headers["Authorization"].FirstOrDefault() ?? string.Empty;
        var userToken = HttpContext.Request.Headers["X-Aizen-User-Token"].FirstOrDefault() ?? string.Empty;
        var result = await _cqrs.ProcessAsync(
            new RejectCompletionCommand(serviceRequestId, request, auth, userToken), ct);
        return SetResponse(result);
    }

    [HttpPatch("service-requests/{serviceRequestId:long}/disputes/{disputeId:long}/status")]
    [ProducesResponseType(typeof(AdminBffCommandResultDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AdminBffCommandResultDto>> ChangeDisputeStatus(
        long serviceRequestId, long disputeId,
        [FromBody] ChangeServiceRequestDisputeStatusRequest request, CancellationToken ct)
    {
        var auth = HttpContext.Request.Headers["Authorization"].FirstOrDefault() ?? string.Empty;
        var userToken = HttpContext.Request.Headers["X-Aizen-User-Token"].FirstOrDefault() ?? string.Empty;
        var result = await _cqrs.ProcessAsync(
            new ChangeDisputeStatusCommand(serviceRequestId, disputeId, request, auth, userToken), ct);
        return SetResponse(result);
    }

    [HttpPatch("service-requests/{serviceRequestId:long}/disputes/{disputeId:long}/resolve")]
    [ProducesResponseType(typeof(ResolveServiceRequestDisputeResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ResolveServiceRequestDisputeResponse>> ResolveDispute(
        long serviceRequestId, long disputeId,
        [FromBody] ResolveServiceRequestDisputeRequest request, CancellationToken ct)
    {
        var auth = HttpContext.Request.Headers["Authorization"].FirstOrDefault() ?? string.Empty;
        var userToken = HttpContext.Request.Headers["X-Aizen-User-Token"].FirstOrDefault() ?? string.Empty;
        var result = await _cqrs.ProcessAsync(
            new ResolveDisputeCommand(serviceRequestId, disputeId, request, auth, userToken), ct);
        return SetResponse(result);
    }
}
