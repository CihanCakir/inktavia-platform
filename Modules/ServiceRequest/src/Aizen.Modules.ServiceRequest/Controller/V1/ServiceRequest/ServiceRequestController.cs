using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Filter;
using Aizen.Modules.ServiceRequest.Abstraction.Request.ServiceRequest;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Dispute;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Owner;
using Aizen.Modules.ServiceRequest.Application.Command.ServiceRequest;
using Aizen.Modules.ServiceRequest.Application.Query.Dispute;
using Aizen.Modules.ServiceRequest.Application.Query.Owner.GetOwnerDisputes;
using Aizen.Modules.ServiceRequest.Application.Query.Owner.GetServiceRequestPaymentStatusForOwner;
using Aizen.Modules.ServiceRequest.Application.Query.ServiceRequest;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Aizen.Modules.ServiceRequest.Controller.V1.ServiceRequest;

[ApiController]
[Route("api/v1/service-requests")]
[Tags("ServiceRequest")]
[Authorize]
[DocumentationInfo("ServiceRequest endpoints", "Boat owner CRUD and lifecycle operations for service requests.")]
public sealed class ServiceRequestController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;
    private readonly IAizenInfoAccessor _info;

    public ServiceRequestController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs, IAizenInfoAccessor info)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
        _info = info;
    }

    // Owner id resolution — prefer the asserted identity that a trusted BFF supplies via the
    // X-Aizen-Bff-Assertion service-token path (AizenUserInfoMiddleware sets UserInfo.UserId). This matches the
    // create/update/cancel/detail handlers, which already read _info.UserInfoAccessor.UserInfo.UserId. Fall back
    // to the NameIdentifier claim for a direct Identity-JWT caller. Without this, `my` would 500 for a BFF
    // caller whose NameIdentifier is the Keycloak service-account subject (non-numeric).
    private long CurrentUserId
    {
        get
        {
            var asserted = _info.UserInfoAccessor?.UserInfo?.UserId ?? 0;
            if (asserted > 0)
                return asserted;

            var raw = ContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return long.TryParse(raw, out var id) ? id : 0;
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateServiceRequestResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CreateServiceRequestResponse?>> Create(
        [FromBody] CreateServiceRequestRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<CreateServiceRequestResponse>(new CreateServiceRequestCommand(req), ct);
        return SetResponse(result);
    }

    [HttpPut("{serviceRequestId:long}")]
    [ProducesResponseType(typeof(UpdateServiceRequestResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<UpdateServiceRequestResponse?>> Update(
        [FromRoute] long serviceRequestId, [FromBody] UpdateServiceRequestRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<UpdateServiceRequestResponse>(new UpdateServiceRequestCommand(serviceRequestId, req), ct);
        return SetResponse(result);
    }

    [HttpGet("{serviceRequestId:long}")]
    [ProducesResponseType(typeof(GetServiceRequestDetailResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetServiceRequestDetailResponse?>> GetDetail(
        [FromRoute] long serviceRequestId, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<GetServiceRequestDetailResponse>(new GetServiceRequestDetailQuery(serviceRequestId), ct);
        return SetResponse(result);
    }

    [HttpGet("my")]
    [ProducesResponseType(typeof(GetServiceRequestListResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetServiceRequestListResponse?>> GetMyList(
        [FromQuery] ServiceRequestListFilterRequest filter, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<GetServiceRequestListResponse>(new GetServiceRequestListQuery(CurrentUserId, filter), ct);
        return SetResponse(result);
    }

    /// <summary>
    /// BE-MO3 — the owner-facing payment status of an accepted service request (poll after accept: Pending →
    /// Paid/Failed). Owner-scoped in the handler (the caller must own the SR). Cost-free: customer total + status
    /// only. Returns <c>None</c> before an offer is accepted (no escrow transaction yet).
    /// </summary>
    [HttpGet("{serviceRequestId:long}/payment-status")]
    [ProducesResponseType(typeof(GetServiceRequestPaymentStatusForOwnerResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetServiceRequestPaymentStatusForOwnerResponse?>> GetPaymentStatus(
        [FromRoute] long serviceRequestId, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<GetServiceRequestPaymentStatusForOwnerResponse>(
            new GetServiceRequestPaymentStatusForOwnerQuery(serviceRequestId), ct);
        return SetResponse(result);
    }

    [HttpPatch("{serviceRequestId:long}/cancel")]
    [ProducesResponseType(typeof(CancelServiceRequestResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CancelServiceRequestResponse?>> Cancel(
        [FromRoute] long serviceRequestId, [FromBody] CancelServiceRequestRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<CancelServiceRequestResponse>(new CancelServiceRequestCommand(serviceRequestId, req), ct);
        return SetResponse(result);
    }

    [HttpPatch("{serviceRequestId:long}/publish")]
    [ProducesResponseType(typeof(UpdateServiceRequestResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<UpdateServiceRequestResponse?>> Publish(
        [FromRoute] long serviceRequestId, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<UpdateServiceRequestResponse>(new PublishServiceRequestCommand(serviceRequestId), ct);
        return SetResponse(result);
    }

    [HttpPost("{serviceRequestId:long}/attachments")]
    [ProducesResponseType(typeof(AddServiceRequestAttachmentResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AddServiceRequestAttachmentResponse?>> AddAttachment(
        [FromRoute] long serviceRequestId, [FromBody] AddServiceRequestAttachmentRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<AddServiceRequestAttachmentResponse>(new AddServiceRequestAttachmentCommand(serviceRequestId, req), ct);
        return SetResponse(result);
    }

    // ── BE-MO5 — owner disputes (read-only) ─────────────────────────────────────
    // The owner opens a dispute via the shared POST .../{srId}/dispute endpoint (actorType resolves to Owner).
    // These two owner-scoped reads mirror the provider surface: a "my disputes" list and the cost-free case file.
    // Resolution / status-change stay Admin-only (ServiceRequestDisputeController); the owner is read-only here.

    /// <summary>
    /// BE-MO5 — the calling owner's own disputes — the disputes on the service requests they own — plus a global
    /// open/actionable count. Owner identity is taken from the trusted context, never from parameters. Cost-free:
    /// no offer economics leave here.
    /// </summary>
    [HttpGet("owner/disputes")]
    [ProducesResponseType(typeof(GetOwnerDisputesResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetOwnerDisputesResponse?>> GetMyDisputes(
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        [FromQuery] ServiceRequestDisputeStatus? status = null,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<GetOwnerDisputesResponse>(
            new GetOwnerDisputesQuery(pageIndex, pageSize) { StatusFilter = status }, ct);
        return SetResponse(result);
    }

    /// <summary>
    /// BE-MO5 — the cost-free dispute case file for the owner (read-only): the same composition an admin sees
    /// (BE-S13a), minus nothing cost-free but never carrying supplier cost / dealer margin. The underlying query is
    /// ungated by design (it takes only <c>disputeId</c>); this owner endpoint gates it by verifying the caller owns
    /// the SR behind the dispute (defense-in-depth — the mobile BFF also owner-gates via <c>EnsureOwnedAsync</c>).
    /// Resolution / status-change are NOT exposed to the owner (Admin-only).
    /// </summary>
    [HttpGet("owner/disputes/{disputeId:long}/case")]
    [ProducesResponseType(typeof(GetDisputeCaseDetailResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetDisputeCaseDetailResponse?>> GetMyDisputeCase(
        [FromRoute] long disputeId, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<GetDisputeCaseDetailResponse>(
            new GetDisputeCaseDetailQuery(disputeId), ct);

        // Owner gate: never leak another owner's (or a provider's) case. Clean not-found on any mismatch.
        if (result?.ServiceRequest is null || result.ServiceRequest.OwnerUserId != CurrentUserId)
            throw new AizenBusinessException("Dispute not found.");

        return SetResponse(result);
    }
}
