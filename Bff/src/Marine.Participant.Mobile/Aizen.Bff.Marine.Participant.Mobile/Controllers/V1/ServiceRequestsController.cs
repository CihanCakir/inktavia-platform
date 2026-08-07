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
    /// (status = Rejected). Accept is deliberately not exposed yet (MO3 wires checkout).</summary>
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
}
