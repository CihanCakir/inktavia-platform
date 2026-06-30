using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Filter;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Offer;
using Aizen.Modules.ServiceRequest.Abstraction.Request.ServiceRequest;
using Aizen.Modules.ServiceRequest.Abstraction.Request.WorkLog;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Admin;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Offer;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;
using Aizen.Modules.ServiceRequest.Abstraction.Response.WorkLog;
using Aizen.Modules.ServiceRequest.Application.Command.Offer;
using Aizen.Modules.ServiceRequest.Application.Command.ServiceRequest;
using Aizen.Modules.ServiceRequest.Application.Command.WorkLog;
using Aizen.Modules.ServiceRequest.Application.Query.Admin;
using Aizen.Modules.ServiceRequest.Application.Query.Offer;
using Aizen.Modules.ServiceRequest.Application.Query.ServiceRequest;
using Aizen.Modules.ServiceRequest.Application.Query.WorkLog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.ServiceRequest.Controller.V1.Admin;

[ApiController]
[Route("api/v1/admin/service-requests")]
[Tags("Admin - ServiceRequest")]
[Authorize(Roles = "Admin")]
[DocumentationInfo("Admin ServiceRequest endpoints", "Admin-level service request management and oversight.")]
public sealed class AdminServiceRequestController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public AdminServiceRequestController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    [HttpGet]
    [ProducesResponseType(typeof(GetAdminServiceRequestListResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetAdminServiceRequestListResponse?>> GetList(
        [FromQuery] AdminServiceRequestFilterRequest filter, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<GetAdminServiceRequestListResponse>(
            new GetAdminServiceRequestListQuery(filter), ct);
        return SetResponse(result);
    }

    [HttpGet("disputes")]
    [ProducesResponseType(typeof(GetAdminDisputeListResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetAdminDisputeListResponse?>> GetDisputeList(
        [FromQuery] AdminDisputeFilterRequest filter, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<GetAdminDisputeListResponse>(
            new GetAdminDisputeListQuery(filter), ct);
        return SetResponse(result);
    }

    [HttpGet("{serviceRequestId:long}")]
    [ProducesResponseType(typeof(GetServiceRequestDetailResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetServiceRequestDetailResponse?>> GetDetail(
        [FromRoute] long serviceRequestId, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<GetServiceRequestDetailResponse>(
            new GetServiceRequestDetailQuery(serviceRequestId), ct);
        return SetResponse(result);
    }

    [HttpPatch("{serviceRequestId:long}/status")]
    [ProducesResponseType(typeof(UpdateServiceRequestStatusResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<UpdateServiceRequestStatusResponse?>> UpdateStatus(
        [FromRoute] long serviceRequestId, [FromBody] UpdateServiceRequestStatusRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<UpdateServiceRequestStatusResponse>(new UpdateServiceRequestStatusCommand(serviceRequestId, req), ct);
        return SetResponse(result);
    }

    [HttpPost("{serviceRequestId:long}/assign")]
    [ProducesResponseType(typeof(AssignProviderResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AssignProviderResponse?>> AssignProvider(
        [FromRoute] long serviceRequestId, [FromBody] AssignProviderRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<AssignProviderResponse>(new AssignProviderCommand(serviceRequestId, req), ct);
        return SetResponse(result);
    }

    [HttpGet("{serviceRequestId:long}/timeline")]
    [ProducesResponseType(typeof(GetServiceRequestTimelineResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetServiceRequestTimelineResponse?>> GetTimeline(
        [FromRoute] long serviceRequestId, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<GetServiceRequestTimelineResponse>(new GetServiceRequestTimelineQuery(serviceRequestId), ct);
        return SetResponse(result);
    }

    [HttpPost("{serviceRequestId:long}/complete")]
    [ProducesResponseType(typeof(CompleteServiceRequestResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CompleteServiceRequestResponse?>> Complete(
        [FromRoute] long serviceRequestId, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<CompleteServiceRequestResponse>(new CompleteServiceRequestCommand(serviceRequestId), ct);
        return SetResponse(result);
    }

    [HttpPost("{serviceRequestId:long}/dispute")]
    [ProducesResponseType(typeof(DisputeServiceRequestResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<DisputeServiceRequestResponse?>> Dispute(
        [FromRoute] long serviceRequestId, [FromBody] DisputeServiceRequestRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<DisputeServiceRequestResponse>(new DisputeServiceRequestCommand(serviceRequestId, req), ct);
        return SetResponse(result);
    }

    [HttpGet("{serviceRequestId:long}/offers")]
    [ProducesResponseType(typeof(GetProviderOffersResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetProviderOffersResponse?>> GetOffers(
        [FromRoute] long serviceRequestId, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<GetProviderOffersResponse>(new GetProviderOffersQuery(serviceRequestId), ct);
        return SetResponse(result);
    }

    [HttpGet("{serviceRequestId:long}/work-logs")]
    [ProducesResponseType(typeof(GetWorkLogsResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetWorkLogsResponse?>> GetWorkLogs(
        [FromRoute] long serviceRequestId, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<GetWorkLogsResponse>(new GetWorkLogsQuery(serviceRequestId), ct);
        return SetResponse(result);
    }

    [HttpPost("{serviceRequestId:long}/work-logs")]
    [ProducesResponseType(typeof(AddWorkLogEntryResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AddWorkLogEntryResponse?>> AddWorkLogEntry(
        [FromRoute] long serviceRequestId, [FromBody] AddWorkLogEntryRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<AddWorkLogEntryResponse>(new AddWorkLogEntryCommand(serviceRequestId, req), ct);
        return SetResponse(result);
    }

    [HttpPatch("{serviceRequestId:long}/work-logs/phases/{phaseNumber:int}")]
    [ProducesResponseType(typeof(UpdateWorkPhaseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<AizenApiResponse<UpdateWorkPhaseResponse?>> UpdateWorkPhase(
        [FromRoute] long serviceRequestId, [FromRoute] int phaseNumber,
        [FromBody] UpdateWorkPhaseRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<UpdateWorkPhaseResponse>(
            new UpdateWorkPhaseCommand(serviceRequestId, phaseNumber, req), ct);
        return SetResponse(result);
    }

    [HttpPost("{serviceRequestId:long}/payment/release")]
    [ProducesResponseType(typeof(ReleasePaymentResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ReleasePaymentResponse?>> ReleasePayment(
        [FromRoute] long serviceRequestId, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<ReleasePaymentResponse>(new ReleasePaymentCommand(serviceRequestId), ct);
        return SetResponse(result);
    }

    [HttpPost("{serviceRequestId:long}/offers/{offerId:long}/accept")]
    [ProducesResponseType(typeof(AcceptServiceRequestOfferResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AcceptServiceRequestOfferResponse?>> AcceptOffer(
        [FromRoute] long serviceRequestId, [FromRoute] long offerId, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<AcceptServiceRequestOfferResponse>(
            new AcceptServiceRequestOfferCommand(serviceRequestId, new AcceptServiceRequestOfferRequest { OfferId = offerId }), ct);
        return SetResponse(result);
    }

    [HttpPost("{serviceRequestId:long}/offers/{offerId:long}/reject")]
    [ProducesResponseType(typeof(RejectServiceRequestOfferResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<RejectServiceRequestOfferResponse?>> RejectOffer(
        [FromRoute] long serviceRequestId, [FromRoute] long offerId, [FromBody] RejectServiceRequestOfferRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<RejectServiceRequestOfferResponse>(
            new RejectServiceRequestOfferCommand(serviceRequestId, req), ct);
        return SetResponse(result);
    }
}
