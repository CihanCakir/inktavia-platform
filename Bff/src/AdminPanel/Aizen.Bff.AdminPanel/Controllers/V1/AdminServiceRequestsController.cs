using Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Command;
using Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Dto;
using Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Query;
using Aizen.Bff.AdminPanel.Application.Common.Dto;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Completion;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Dispute;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Offer;
using Aizen.Modules.ServiceRequest.Abstraction.Request.ServiceRequest;
using Aizen.Modules.ServiceRequest.Abstraction.Request.WorkLog;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Admin;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Completion;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Conversation;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Dispute;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Offer;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;
using Aizen.Modules.ServiceRequest.Abstraction.Response.WorkLog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.AdminPanel.Controllers.V1;

[ApiController]
[Route("api/v1/admin-panel")]
[Tags("Admin Panel - Service Requests")]
[Authorize(Policy = "AdminPanelAccess")]
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
        var result = await _cqrs.ProcessAsync(
            new GetAdminServiceRequestListQuery(status, vesselId, pageIndex, pageSize), ct);
        return SetResponse(result);
    }

    [HttpGet("service-requests/by-vessel/{vesselId:long}/history")]
    [ProducesResponseType(typeof(ServiceRequestVesselHistoryBffResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ServiceRequestVesselHistoryBffResponse>> GetServiceHistoryByVessel(
        long vesselId,
        [FromQuery] int take = 10,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new GetServiceRequestsByVesselHistoryQuery(vesselId, take), ct);
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
        var result = await _cqrs.ProcessAsync(
            new GetAdminDisputeListQuery(status, pageIndex, pageSize), ct);
        return SetResponse(result);
    }

    [HttpGet("service-requests/{serviceRequestId:long}")]
    [ProducesResponseType(typeof(AdminServiceRequestOperationDetailResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AdminServiceRequestOperationDetailResponse>> GetServiceRequestDetail(
        long serviceRequestId, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new GetAdminServiceRequestOperationDetailQuery(serviceRequestId), ct);
        return SetResponse(result);
    }

    [HttpPatch("service-requests/{serviceRequestId:long}/cancel")]
    [ProducesResponseType(typeof(CancelServiceRequestResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CancelServiceRequestResponse>> CancelServiceRequest(
        long serviceRequestId, [FromBody] CancelServiceRequestRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new CancelServiceRequestCommand(serviceRequestId, request), ct);
        return SetResponse(result);
    }

    [HttpPatch("service-requests/{serviceRequestId:long}/completion/approve")]
    [ProducesResponseType(typeof(ApproveServiceRequestCompletionResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ApproveServiceRequestCompletionResponse>> ApproveCompletion(
        long serviceRequestId, [FromBody] ApproveServiceRequestCompletionRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new ApproveCompletionCommand(serviceRequestId, request), ct);
        return SetResponse(result);
    }

    [HttpPatch("service-requests/{serviceRequestId:long}/completion/reject")]
    [ProducesResponseType(typeof(RejectServiceRequestCompletionResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<RejectServiceRequestCompletionResponse>> RejectCompletion(
        long serviceRequestId, [FromBody] RejectServiceRequestCompletionRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new RejectCompletionCommand(serviceRequestId, request), ct);
        return SetResponse(result);
    }

    [HttpPatch("service-requests/{serviceRequestId:long}/disputes/{disputeId:long}/status")]
    [ProducesResponseType(typeof(AdminBffCommandResultDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AdminBffCommandResultDto>> ChangeDisputeStatus(
        long serviceRequestId, long disputeId,
        [FromBody] ChangeServiceRequestDisputeStatusRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new ChangeDisputeStatusCommand(serviceRequestId, disputeId, request), ct);
        return SetResponse(result);
    }

    [HttpPatch("service-requests/{serviceRequestId:long}/disputes/{disputeId:long}/resolve")]
    [ProducesResponseType(typeof(ResolveServiceRequestDisputeResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ResolveServiceRequestDisputeResponse>> ResolveDispute(
        long serviceRequestId, long disputeId,
        [FromBody] ResolveServiceRequestDisputeRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new ResolveDisputeCommand(serviceRequestId, disputeId, request), ct);
        return SetResponse(result);
    }

    [HttpGet("service-requests/filter-options")]
    [ProducesResponseType(typeof(AdminServiceRequestFilterOptionsResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AdminServiceRequestFilterOptionsResponse>> GetFilterOptions(CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new GetAdminServiceRequestFilterOptionsQuery(), ct);
        return SetResponse(result);
    }

    [HttpGet("service-requests/{serviceRequestId:long}/timeline")]
    [ProducesResponseType(typeof(AdminServiceRequestTimelineResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AdminServiceRequestTimelineResponse>> GetTimeline(
        long serviceRequestId, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new GetAdminServiceRequestTimelineQuery(serviceRequestId), ct);
        return SetResponse(result);
    }

    [HttpPatch("service-requests/{serviceRequestId:long}/status")]
    [ProducesResponseType(typeof(UpdateServiceRequestStatusResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<UpdateServiceRequestStatusResponse>> UpdateStatus(
        long serviceRequestId, [FromBody] UpdateServiceRequestStatusRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new UpdateServiceRequestStatusCommand(serviceRequestId, request), ct);
        return SetResponse(result);
    }

    [HttpPost("service-requests/{serviceRequestId:long}/assign")]
    [ProducesResponseType(typeof(AssignProviderResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AssignProviderResponse>> AssignProvider(
        long serviceRequestId, [FromBody] AssignProviderRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new AssignServiceRequestProviderCommand(serviceRequestId, request), ct);
        return SetResponse(result);
    }

    [HttpPost("service-requests/{serviceRequestId:long}/complete")]
    [ProducesResponseType(typeof(CompleteServiceRequestResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CompleteServiceRequestResponse>> Complete(
        long serviceRequestId, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new CompleteServiceRequestAdminCommand(serviceRequestId), ct);
        return SetResponse(result);
    }

    [HttpPost("service-requests/{serviceRequestId:long}/dispute")]
    [ProducesResponseType(typeof(DisputeServiceRequestResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<DisputeServiceRequestResponse>> Dispute(
        long serviceRequestId, [FromBody] DisputeServiceRequestRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new DisputeServiceRequestAdminCommand(serviceRequestId, request), ct);
        return SetResponse(result);
    }

    [HttpGet("service-requests/{serviceRequestId:long}/offers")]
    [ProducesResponseType(typeof(AdminServiceRequestOffersResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AdminServiceRequestOffersResponse>> GetOffers(
        long serviceRequestId, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new GetAdminServiceRequestOffersQuery(serviceRequestId), ct);
        return SetResponse(result);
    }

    [HttpPost("service-requests/{serviceRequestId:long}/offers/{offerId:long}/accept")]
    [ProducesResponseType(typeof(AcceptServiceRequestOfferResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AcceptServiceRequestOfferResponse>> AcceptOffer(
        long serviceRequestId, long offerId, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new AcceptServiceRequestOfferAdminCommand(serviceRequestId, offerId), ct);
        return SetResponse(result);
    }

    [HttpPost("service-requests/{serviceRequestId:long}/offers/{offerId:long}/reject")]
    [ProducesResponseType(typeof(RejectServiceRequestOfferResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<RejectServiceRequestOfferResponse>> RejectOffer(
        long serviceRequestId, long offerId, [FromBody] RejectServiceRequestOfferRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new RejectServiceRequestOfferAdminCommand(serviceRequestId, offerId, request), ct);
        return SetResponse(result);
    }

    [HttpGet("service-requests/{serviceRequestId:long}/work-logs")]
    [ProducesResponseType(typeof(AdminWorkLogsResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AdminWorkLogsResponse>> GetWorkLogs(
        long serviceRequestId, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new GetAdminWorkLogsQuery(serviceRequestId), ct);
        return SetResponse(result);
    }

    [HttpPost("service-requests/{serviceRequestId:long}/work-logs")]
    [ProducesResponseType(typeof(AddWorkLogEntryResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AddWorkLogEntryResponse>> AddWorkLogEntry(
        long serviceRequestId, [FromBody] AddWorkLogEntryRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new AddWorkLogEntryAdminCommand(serviceRequestId, request), ct);
        return SetResponse(result);
    }

    [HttpPatch("service-requests/{serviceRequestId:long}/work-logs/phases/{phaseNumber:int}")]
    [ProducesResponseType(typeof(UpdateWorkPhaseResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<UpdateWorkPhaseResponse>> UpdateWorkPhase(
        long serviceRequestId, int phaseNumber, [FromBody] UpdateWorkPhaseRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new UpdateWorkPhaseAdminCommand(serviceRequestId, phaseNumber, request), ct);
        return SetResponse(result);
    }

    [HttpPost("service-requests/{serviceRequestId:long}/payment/release")]
    [ProducesResponseType(typeof(ReleasePaymentResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ReleasePaymentResponse>> ReleasePayment(
        long serviceRequestId, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new ReleasePaymentAdminCommand(serviceRequestId), ct);
        return SetResponse(result);
    }

    [HttpGet("messages/conversations")]
    [ProducesResponseType(typeof(AdminConversationsResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AdminConversationsResponse>> GetConversations(
        [FromQuery] string? filter, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new GetAdminConversationsQuery(filter), ct);
        return SetResponse(result);
    }

    [HttpGet("messages/conversations/{conversationId:long}")]
    [ProducesResponseType(typeof(AdminConversationDetailResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AdminConversationDetailResponse>> GetConversationDetail(
        long conversationId, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new GetAdminConversationDetailQuery(conversationId), ct);
        return SetResponse(result);
    }
}
