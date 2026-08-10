using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Authorization;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.ServiceRequest;
using Aizen.Bff.Marine.Participant.Mobile.Application.ServiceRequest;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.Marine.Participant.Mobile.Controllers.V1;

/// <summary>
/// Participant-scoped service requests (BE_MO1): create / list / detail / update / cancel / attach. Identity is
/// resolved server-side from the validated token (never the body) and asserted to the module; ownership is gated
/// BFF-side for every by-id action. Cost-free — offers/economics arrive in MO2.
/// </summary>
[ApiController]
[Route("api/v1/mobile/service-requests")]
[Tags("Mobile - Service Requests")]
[Authorize(Policy = ParticipantAuthorizationPolicies.ParticipantAuthenticated)]
public sealed class ServiceRequestsController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public ServiceRequestsController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    /// <summary>The caller's own service requests (paged; empty when none).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(MobileServiceRequestListDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MobileServiceRequestListDto>> GetMy(
        [FromQuery] int pageIndex = 0, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new GetMobileMyServiceRequestsQuery(pageIndex, pageSize), ct);
        return SetResponse(result);
    }

    /// <summary>Full detail for one of the caller's own requests; a foreign/unknown id yields a clean not-found.</summary>
    [HttpGet("{serviceRequestId:long}")]
    [ProducesResponseType(typeof(MobileServiceRequestDetailDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MobileServiceRequestDetailDto>> GetDetail(
        [FromRoute] long serviceRequestId, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new GetMobileServiceRequestDetailQuery(serviceRequestId), ct);
        return SetResponse(result);
    }

    /// <summary>Create a service request for the caller (optionally publishing + attaching uploaded files);
    /// returns the assembled detail reflecting exactly what persisted.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(MobileServiceRequestDetailDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MobileServiceRequestDetailDto>> Create(
        [FromBody] CreateMobileServiceRequestRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new CreateMobileServiceRequestCommand(request), ct);
        return SetResponse(result);
    }

    /// <summary>Update one of the caller's own (Draft) requests; returns the updated detail.</summary>
    [HttpPut("{serviceRequestId:long}")]
    [ProducesResponseType(typeof(MobileServiceRequestDetailDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MobileServiceRequestDetailDto>> Update(
        [FromRoute] long serviceRequestId, [FromBody] UpdateMobileServiceRequestRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new UpdateMobileServiceRequestCommand(serviceRequestId, request), ct);
        return SetResponse(result);
    }

    /// <summary>Cancel one of the caller's own requests with the N-E structured reason + optional note; returns
    /// the re-read detail (status = Cancelled).</summary>
    [HttpPost("{serviceRequestId:long}/cancel")]
    [ProducesResponseType(typeof(MobileServiceRequestDetailDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MobileServiceRequestDetailDto>> Cancel(
        [FromRoute] long serviceRequestId, [FromBody] CancelMobileServiceRequestRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new CancelMobileServiceRequestCommand(serviceRequestId, request ?? new CancelMobileServiceRequestRequest()), ct);
        return SetResponse(result);
    }

    /// <summary>Attach a completed client-side upload (fileId) to one of the caller's own requests.</summary>
    [HttpPost("{serviceRequestId:long}/attachments")]
    [ProducesResponseType(typeof(MobileServiceRequestAttachmentDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MobileServiceRequestAttachmentDto>> AddAttachment(
        [FromRoute] long serviceRequestId, [FromBody] AddMobileServiceRequestAttachmentRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new AddMobileServiceRequestAttachmentCommand(serviceRequestId, request), ct);
        return SetResponse(result);
    }

    // ── BE_MO2 — owner offers inbox (cost-free; no money — accept → checkout is MO3) ────────────────────

    /// <summary>The provider offers received on one of the caller's own requests (cost-free breakdown; empty when
    /// none). Owner-scoped; a foreign/unknown id yields a clean not-found.</summary>
    [HttpGet("{serviceRequestId:long}/offers")]
    [ProducesResponseType(typeof(List<MobileServiceRequestOfferDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<List<MobileServiceRequestOfferDto>>> GetOffers(
        [FromRoute] long serviceRequestId, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new GetMobileServiceRequestOffersQuery(serviceRequestId), ct);
        return SetResponse(result);
    }

    /// <summary>One received offer with its full cost-free breakdown (line items, KDV, discount, customer total, S3 FX).</summary>
    [HttpGet("{serviceRequestId:long}/offers/{offerId:long}")]
    [ProducesResponseType(typeof(MobileServiceRequestOfferDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MobileServiceRequestOfferDto>> GetOffer(
        [FromRoute] long serviceRequestId, [FromRoute] long offerId, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new GetMobileServiceRequestOfferQuery(serviceRequestId, offerId), ct);
        return SetResponse(result);
    }

    /// <summary>Reject a received offer with the N-E structured reason + optional note; returns the re-read offer
    /// (status = Rejected).</summary>
    [HttpPost("{serviceRequestId:long}/offers/{offerId:long}/reject")]
    [ProducesResponseType(typeof(MobileServiceRequestOfferDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MobileServiceRequestOfferDto>> RejectOffer(
        [FromRoute] long serviceRequestId, [FromRoute] long offerId,
        [FromBody] RejectMobileOfferRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new RejectMobileServiceRequestOfferCommand(serviceRequestId, offerId, request ?? new RejectMobileOfferRequest()), ct);
        return SetResponse(result);
    }

    // ── BE_MO3 — owner accept + pay + status (money moves) ──────────────────────────────────────────────

    /// <summary>Accept a received offer → the module runs BE-P8 economics + escrow and captures at accept (manual
    /// gateway in dev; iyzico when the P9 keys are present). Returns the accepted offer's SR + the payment status
    /// right after accept (dev/manual → already Paid). A blocked accept (Rejected/ConfigError) surfaces as an error
    /// — no half-accepted, unpaid SR. Owner-gated; amounts are server-owned (the body is empty).</summary>
    [HttpPost("{serviceRequestId:long}/offers/{offerId:long}/accept")]
    [ProducesResponseType(typeof(MobileAcceptOfferResultDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MobileAcceptOfferResultDto>> AcceptOffer(
        [FromRoute] long serviceRequestId, [FromRoute] long offerId, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new AcceptMobileServiceRequestOfferCommand(serviceRequestId, offerId), ct);
        return SetResponse(result);
    }

    /// <summary>Poll the payment lifecycle of the caller's accepted SR (Pending → Paid/Failed). Owner-gated;
    /// cost-free (customer total + status only). Returns <c>None</c> before any offer is accepted.</summary>
    [HttpGet("{serviceRequestId:long}/payment-status")]
    [ProducesResponseType(typeof(MobilePaymentStatusDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MobilePaymentStatusDto>> GetPaymentStatus(
        [FromRoute] long serviceRequestId, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new GetMobileServiceRequestPaymentStatusQuery(serviceRequestId), ct);
        return SetResponse(result);
    }

    // ── BE_MO4 — owner completion review (reuses approve/reject + the decoupled escrow release) ──────────

    /// <summary>The provider's completion on one of the caller's own requests — status, notes, evidence (with a
    /// freshly-minted access URL), submitted-at, and the N3 auto-approve deadline for the countdown. Owner-scoped;
    /// a foreign/unknown id yields a clean not-found; <c>null</c> when no completion has been submitted yet.</summary>
    [HttpGet("{serviceRequestId:long}/completion")]
    [ProducesResponseType(typeof(MobileServiceRequestCompletionDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MobileServiceRequestCompletionDto>> GetCompletion(
        [FromRoute] long serviceRequestId, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new GetMobileServiceRequestCompletionQuery(serviceRequestId), ct);
        return SetResponse(result);
    }

    /// <summary>Approve the provider's completion (optional 1..5 rating + note) → the module transitions the SR to
    /// Completed and the existing decoupled escrow release runs (untouched). Returns the re-read completion
    /// (status = ApprovedByOwner). Owner-gated; an owner action before the deadline cancels the N3 auto-approval.</summary>
    [HttpPost("{serviceRequestId:long}/completion/approve")]
    [ProducesResponseType(typeof(MobileServiceRequestCompletionDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MobileServiceRequestCompletionDto>> ApproveCompletion(
        [FromRoute] long serviceRequestId, [FromBody] ApproveMobileCompletionRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new ApproveMobileServiceRequestCompletionCommand(serviceRequestId, request ?? new ApproveMobileCompletionRequest()), ct);
        return SetResponse(result);
    }

    /// <summary>Reject the provider's completion with the N-E structured reason + optional note (SR → InProgress;
    /// no money). Returns the re-read completion (status = RejectedByOwner). Owner-gated.</summary>
    [HttpPost("{serviceRequestId:long}/completion/reject")]
    [ProducesResponseType(typeof(MobileServiceRequestCompletionDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MobileServiceRequestCompletionDto>> RejectCompletion(
        [FromRoute] long serviceRequestId, [FromBody] RejectMobileCompletionRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new RejectMobileServiceRequestCompletionCommand(serviceRequestId, request ?? new RejectMobileCompletionRequest()), ct);
        return SetResponse(result);
    }

    // ── BE_MO5 — owner disputes (open + my-disputes list + read-only cost-free case) ─────────────────────
    // The owner opens a dispute, sees THEIR disputes, and reads the composed case. Resolution / status-change
    // stay Admin-only — there is deliberately no owner resolve/status endpoint here (the owner is read-only; N3
    // notifies them of the outcome).

    /// <summary>Open a dispute (N-E structured reason + description) on one of the caller's own requests. Owner-gated;
    /// a foreign/unknown id yields a clean not-found. Returns the just-opened dispute as a list row.</summary>
    [HttpPost("{serviceRequestId:long}/dispute")]
    [ProducesResponseType(typeof(MobileDisputeListItemDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MobileDisputeListItemDto>> OpenDispute(
        [FromRoute] long serviceRequestId, [FromBody] OpenMobileDisputeRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new OpenMobileDisputeCommand(serviceRequestId, request ?? new OpenMobileDisputeRequest()), ct);
        return SetResponse(result);
    }

    /// <summary>The caller-owner's own disputes (paged; empty when none) + a global open/actionable count. Optionally
    /// narrowed to one ServiceRequestDisputeStatus name. Cost-free.</summary>
    [HttpGet("disputes")]
    [ProducesResponseType(typeof(MobileDisputeListDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MobileDisputeListDto>> GetMyDisputes(
        [FromQuery] int pageIndex = 0, [FromQuery] int pageSize = 20, [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new GetMobileOwnerDisputesQuery(pageIndex, pageSize, status), ct);
        return SetResponse(result);
    }

    /// <summary>The read-only cost-free dispute case for one of the caller's own requests: SR summary, customer-facing
    /// economics, evidence (with access URLs), messages, the lifecycle timeline, and — once resolved — the resolution
    /// outcome. Owner-gated; a foreign/unknown id yields a clean not-found. Resolution / status-change are Admin-only.</summary>
    [HttpGet("{serviceRequestId:long}/dispute/{disputeId:long}/case")]
    [ProducesResponseType(typeof(MobileDisputeCaseDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MobileDisputeCaseDto>> GetDisputeCase(
        [FromRoute] long serviceRequestId, [FromRoute] long disputeId, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new GetMobileDisputeCaseQuery(serviceRequestId, disputeId), ct);
        return SetResponse(result);
    }

    // ── BE_MO6 — owner change orders (review + approve→incremental checkout / decrease-refund + reject) ────

    /// <summary>The change orders on one of the caller's own accepted requests — direction, changed lines, the
    /// incremental ₺, status — plus the derived effective total. Owner-gated; cost-free.</summary>
    [HttpGet("{serviceRequestId:long}/change-orders")]
    [ProducesResponseType(typeof(MobileChangeOrderListDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MobileChangeOrderListDto>> GetChangeOrders(
        [FromRoute] long serviceRequestId, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new GetMobileChangeOrdersQuery(serviceRequestId), ct);
        return SetResponse(result);
    }

    /// <summary>Approve a change order → the S11 apply engine runs (Increase → new incremental snapshot +
    /// capture-at-approve; Decrease → P10 refund of the delta; a P5/S9 breach flips it to Rejected, no capture).
    /// Idempotent (no double-charge on re-approve). Returns the applied CO + the incremental payment status
    /// (already Paid on the manual gateway; pollable for live iyzico). Owner-gated.</summary>
    [HttpPost("{serviceRequestId:long}/change-orders/{changeOrderId:long}/approve")]
    [ProducesResponseType(typeof(MobileChangeOrderApproveResultDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MobileChangeOrderApproveResultDto>> ApproveChangeOrder(
        [FromRoute] long serviceRequestId, [FromRoute] long changeOrderId, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new ApproveMobileChangeOrderCommand(serviceRequestId, changeOrderId), ct);
        return SetResponse(result);
    }

    /// <summary>Reject a proposed change order (optional free-text reason). Terminal; no economics. Owner-gated.</summary>
    [HttpPost("{serviceRequestId:long}/change-orders/{changeOrderId:long}/reject")]
    [ProducesResponseType(typeof(MobileChangeOrderDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MobileChangeOrderDto>> RejectChangeOrder(
        [FromRoute] long serviceRequestId, [FromRoute] long changeOrderId,
        [FromBody] MobileRejectChangeOrderRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new RejectMobileChangeOrderCommand(serviceRequestId, changeOrderId, request ?? new MobileRejectChangeOrderRequest()), ct);
        return SetResponse(result);
    }

    /// <summary>The incremental escrow transaction's payment status for an approved Increase (poll: Pending →
    /// Paid/Failed for the live-iyzico path). Owner-gated; cost-free; None until the CO has an incremental transaction.</summary>
    [HttpGet("{serviceRequestId:long}/change-orders/{changeOrderId:long}/payment-status")]
    [ProducesResponseType(typeof(MobilePaymentStatusDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MobilePaymentStatusDto>> GetChangeOrderPaymentStatus(
        [FromRoute] long serviceRequestId, [FromRoute] long changeOrderId, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new GetMobileChangeOrderPaymentStatusQuery(serviceRequestId, changeOrderId), ct);
        return SetResponse(result);
    }
}
