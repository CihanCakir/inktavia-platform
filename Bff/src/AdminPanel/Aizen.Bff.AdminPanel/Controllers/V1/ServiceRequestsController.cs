using Aizen.Bff.AdminPanel.Application.ServiceRequests.Command;
using Aizen.Bff.AdminPanel.Application.ServiceRequests.Dto;
using Aizen.Bff.AdminPanel.Application.ServiceRequests.Query;
using Aizen.Modules.ServiceRequest.Abstraction.Dto.Pricing;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Pricing;
using Aizen.Bff.AdminPanel.Application.ProfilePerformance.Dto;
using Aizen.Bff.AdminPanel.Application.Common.Dto;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Completion;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Dispute;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Maintenance;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Offer;
using Aizen.Modules.ServiceRequest.Abstraction.Request.ServiceRequest;
using Aizen.Modules.ServiceRequest.Abstraction.Request.WorkLog;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Admin;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Completion;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Dispute;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Maintenance;
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

    public ServiceRequestsController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor) => _cqrs = cqrs;

    [HttpGet("service-requests")]
    [ProducesResponseType(typeof(AdminServiceRequestListResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AdminServiceRequestListResponse>> GetServiceRequests(
        [FromQuery] string? status,
        [FromQuery] long?   vesselId,
        [FromQuery] long?   ownerUserId = null,
        [FromQuery] int     pageIndex   = 0,
        [FromQuery] int     pageSize    = 20,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new GetServiceRequestListBffQuery(status, vesselId, ownerUserId, pageIndex, pageSize), ct);
        return SetResponse(result);
    }

    // ── S2a — pricing attribute definitions (admin CRUD) ─────────────────────────────────────────────────
    [HttpGet("service-requests/pricing-attributes")]
    [ProducesResponseType(typeof(List<PricingAttributeDefinitionDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<List<PricingAttributeDefinitionDto>>> GetPricingAttributes(
        [FromQuery] string? serviceCategoryCode = null, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(
            new ListPricingAttributesBffQuery { ServiceCategoryCode = serviceCategoryCode }, ct));

    [HttpPost("service-requests/pricing-attributes")]
    [ProducesResponseType(typeof(PricingAttributeDefinitionDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<PricingAttributeDefinitionDto>> CreatePricingAttribute(
        [FromBody] PricingAttributeDefinitionRequest request, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new CreatePricingAttributeBffCommand { Request = request }, ct));

    [HttpPut("service-requests/pricing-attributes/{id:long}")]
    [ProducesResponseType(typeof(PricingAttributeDefinitionDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<PricingAttributeDefinitionDto>> UpdatePricingAttribute(
        long id, [FromBody] PricingAttributeDefinitionRequest request, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new UpdatePricingAttributeBffCommand { Id = id, Request = request }, ct));

    [HttpDelete("service-requests/pricing-attributes/{id:long}")]
    [ProducesResponseType(typeof(DeletePricingAttributeResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<DeletePricingAttributeResult>> DeletePricingAttribute(
        long id, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new DeletePricingAttributeBffCommand { Id = id }, ct));

    [HttpGet("service-requests/by-vessel/{vesselId:long}/history")]
    [ProducesResponseType(typeof(ServiceRequestVesselHistoryBffResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ServiceRequestVesselHistoryBffResponse>> GetServiceHistoryByVessel(
        long vesselId,
        [FromQuery] int take = 10,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new GetServiceRequestsByVesselHistoryBffQuery(vesselId, take), ct);
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
            new GetDisputeListBffQuery(status, pageIndex, pageSize), ct);
        return SetResponse(result);
    }

    [HttpGet("service-requests/{serviceRequestId:long}")]
    [ProducesResponseType(typeof(AdminServiceRequestOperationDetailResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AdminServiceRequestOperationDetailResponse>> GetServiceRequestDetail(
        long serviceRequestId, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new GetServiceRequestOperationDetailBffQuery(serviceRequestId), ct);
        return SetResponse(result);
    }

    [HttpPatch("service-requests/{serviceRequestId:long}/cancel")]
    [ProducesResponseType(typeof(CancelServiceRequestResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CancelServiceRequestResponse>> CancelServiceRequest(
        long serviceRequestId, [FromBody] CancelServiceRequestRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new CancelServiceRequestBffCommand(serviceRequestId, request), ct);
        return SetResponse(result);
    }

    [HttpPatch("service-requests/{serviceRequestId:long}/completion/approve")]
    [ProducesResponseType(typeof(ApproveServiceRequestCompletionResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ApproveServiceRequestCompletionResponse>> ApproveCompletion(
        long serviceRequestId, [FromBody] ApproveServiceRequestCompletionRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new ApproveCompletionBffCommand(serviceRequestId, request), ct);
        return SetResponse(result);
    }

    [HttpPatch("service-requests/{serviceRequestId:long}/completion/reject")]
    [ProducesResponseType(typeof(RejectServiceRequestCompletionResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<RejectServiceRequestCompletionResponse>> RejectCompletion(
        long serviceRequestId, [FromBody] RejectServiceRequestCompletionRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new RejectCompletionBffCommand(serviceRequestId, request), ct);
        return SetResponse(result);
    }

    [HttpGet("service-requests/{serviceRequestId:long}/disputes/{disputeId:long}/case")]
    [ProducesResponseType(typeof(GetDisputeCaseDetailResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetDisputeCaseDetailResponse>> GetDisputeCase(
        long serviceRequestId, long disputeId, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new GetDisputeCaseBffQuery(serviceRequestId, disputeId), ct);
        return SetResponse(result);
    }

    [HttpPatch("service-requests/{serviceRequestId:long}/disputes/{disputeId:long}/status")]
    [ProducesResponseType(typeof(AdminBffCommandResultDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AdminBffCommandResultDto>> ChangeDisputeStatus(
        long serviceRequestId, long disputeId,
        [FromBody] ChangeServiceRequestDisputeStatusRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new ChangeDisputeStatusBffCommand(serviceRequestId, disputeId, request), ct);
        return SetResponse(result);
    }

    [HttpPatch("service-requests/{serviceRequestId:long}/disputes/{disputeId:long}/resolve")]
    [ProducesResponseType(typeof(ResolveServiceRequestDisputeResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ResolveServiceRequestDisputeResponse>> ResolveDispute(
        long serviceRequestId, long disputeId,
        [FromBody] ResolveServiceRequestDisputeRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new ResolveDisputeBffCommand(serviceRequestId, disputeId, request), ct);
        return SetResponse(result);
    }

    [HttpGet("service-requests/filter-options")]
    [ProducesResponseType(typeof(AdminServiceRequestFilterOptionsResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AdminServiceRequestFilterOptionsResponse>> GetFilterOptions(CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new GetServiceRequestFilterOptionsBffQuery(), ct);
        return SetResponse(result);
    }

    [HttpGet("service-requests/{serviceRequestId:long}/timeline")]
    [ProducesResponseType(typeof(AdminServiceRequestTimelineResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AdminServiceRequestTimelineResponse>> GetTimeline(
        long serviceRequestId, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new GetServiceRequestTimelineBffQuery(serviceRequestId), ct);
        return SetResponse(result);
    }

    [HttpPatch("service-requests/{serviceRequestId:long}/status")]
    [ProducesResponseType(typeof(UpdateServiceRequestStatusResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<UpdateServiceRequestStatusResponse>> UpdateStatus(
        long serviceRequestId, [FromBody] UpdateServiceRequestStatusRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new UpdateServiceRequestStatusBffCommand(serviceRequestId, request), ct);
        return SetResponse(result);
    }

    [HttpPost("service-requests/{serviceRequestId:long}/assign")]
    [ProducesResponseType(typeof(AssignProviderResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AssignProviderResponse>> AssignProvider(
        long serviceRequestId, [FromBody] AssignProviderRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new AssignServiceRequestProviderBffCommand(serviceRequestId, request), ct);
        return SetResponse(result);
    }

    [HttpPost("service-requests/{serviceRequestId:long}/complete")]
    [ProducesResponseType(typeof(CompleteServiceRequestResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CompleteServiceRequestResponse>> Complete(
        long serviceRequestId, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new CompleteServiceRequestBffCommand(serviceRequestId), ct);
        return SetResponse(result);
    }

    [HttpPost("service-requests/{serviceRequestId:long}/dispute")]
    [ProducesResponseType(typeof(DisputeServiceRequestResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<DisputeServiceRequestResponse>> Dispute(
        long serviceRequestId, [FromBody] DisputeServiceRequestRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new DisputeServiceRequestBffCommand(serviceRequestId, request), ct);
        return SetResponse(result);
    }

    [HttpGet("service-requests/{serviceRequestId:long}/offers")]
    [ProducesResponseType(typeof(AdminServiceRequestOffersResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AdminServiceRequestOffersResponse>> GetOffers(
        long serviceRequestId, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new GetServiceRequestOffersBffQuery(serviceRequestId), ct);
        return SetResponse(result);
    }

    /// <summary>
    /// Phase 23 — Admin provider recommendation preview.
    /// Decision-support only: returns ranked provider candidates from the Profile priority-preview engine.
    /// Candidates are sourced from submitted offers for this SR.
    /// No automatic assignment, no offer creation, no provider notifications.
    /// </summary>
    [HttpPost("service-requests/{serviceRequestId:long}/provider-recommendations/preview")]
    [ProducesResponseType(typeof(SrProviderRecommendationPreviewBffResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<SrProviderRecommendationPreviewBffResponse>> GetProviderRecommendationPreview(
        long serviceRequestId,
        [FromBody] SrProviderRecommendationPreviewBffRequest request,
        CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new GetSrProviderRecommendationPreviewBffQuery(serviceRequestId, request), ct);
        return SetResponse(result);
    }

    [HttpPost("service-requests/{serviceRequestId:long}/offers/{offerId:long}/accept")]
    [ProducesResponseType(typeof(AcceptServiceRequestOfferResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AcceptServiceRequestOfferResponse>> AcceptOffer(
        long serviceRequestId, long offerId, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new AcceptServiceRequestOfferBffCommand(serviceRequestId, offerId), ct);
        return SetResponse(result);
    }

    [HttpPost("service-requests/{serviceRequestId:long}/offers/{offerId:long}/reject")]
    [ProducesResponseType(typeof(RejectServiceRequestOfferResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<RejectServiceRequestOfferResponse>> RejectOffer(
        long serviceRequestId, long offerId, [FromBody] RejectServiceRequestOfferRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new RejectServiceRequestOfferBffCommand(serviceRequestId, offerId, request), ct);
        return SetResponse(result);
    }

    [HttpGet("service-requests/{serviceRequestId:long}/work-logs")]
    [ProducesResponseType(typeof(AdminWorkLogsResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AdminWorkLogsResponse>> GetWorkLogs(
        long serviceRequestId, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new GetWorkLogsBffQuery(serviceRequestId), ct);
        return SetResponse(result);
    }

    [HttpPost("service-requests/{serviceRequestId:long}/work-logs")]
    [ProducesResponseType(typeof(AddWorkLogEntryResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AddWorkLogEntryResponse>> AddWorkLogEntry(
        long serviceRequestId, [FromBody] AddWorkLogEntryRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new AddWorkLogEntryBffCommand(serviceRequestId, request), ct);
        return SetResponse(result);
    }

    [HttpPatch("service-requests/{serviceRequestId:long}/work-logs/phases/{phaseNumber:int}")]
    [ProducesResponseType(typeof(UpdateWorkPhaseResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<UpdateWorkPhaseResponse>> UpdateWorkPhase(
        long serviceRequestId, int phaseNumber, [FromBody] UpdateWorkPhaseRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new UpdateWorkPhaseBffCommand(serviceRequestId, phaseNumber, request), ct);
        return SetResponse(result);
    }

    [HttpPost("service-requests/{serviceRequestId:long}/payment/release")]
    [ProducesResponseType(typeof(ReleasePaymentResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ReleasePaymentResponse>> ReleasePayment(
        long serviceRequestId, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new ReleasePaymentBffCommand(serviceRequestId), ct);
        return SetResponse(result);
    }

    // ── S12 — recurring maintenance schedules (admin upsert + list + active toggle) ──────────────────────
    [HttpGet("service-requests/maintenance-schedules")]
    [ProducesResponseType(typeof(GetMaintenanceScheduleListResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetMaintenanceScheduleListResponse>> GetMaintenanceSchedules(
        [FromQuery] long? vesselId,
        [FromQuery] bool includeInactive = true,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new GetMaintenanceScheduleListBffQuery(vesselId, includeInactive), ct);
        return SetResponse(result);
    }

    [HttpPost("service-requests/maintenance-schedules")]
    [ProducesResponseType(typeof(UpsertMaintenanceScheduleResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<UpsertMaintenanceScheduleResponse>> UpsertMaintenanceSchedule(
        [FromBody] UpsertMaintenanceScheduleRequest request, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new UpsertMaintenanceScheduleBffCommand(request), ct);
        return SetResponse(result);
    }

    [HttpPut("service-requests/maintenance-schedules/{id:long}/active")]
    [ProducesResponseType(typeof(SetMaintenanceScheduleActiveResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<SetMaintenanceScheduleActiveResponse>> SetMaintenanceScheduleActive(
        long id, [FromBody] SetMaintenanceScheduleActiveRequest request, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new SetMaintenanceScheduleActiveBffCommand(id, request), ct);
        return SetResponse(result);
    }

    // BE_WC3c — removed the admin SR-backed conversation audit endpoints (GET /admin-panel/messages/conversations[/{id}]):
    // dead after the read cutover. Admin conversation audit reads from Messaging (/admin-panel/messaging/*).
}
