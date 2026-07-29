using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Offer;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Offer;
using Aizen.Modules.ServiceRequest.Application.Command.Offer;
using Aizen.Modules.ServiceRequest.Application.Command.Offer.SaveOfferDraft;
using Aizen.Modules.ServiceRequest.Application.Command.Offer.PreviewOffer;
using Aizen.Modules.ServiceRequest.Application.Command.Offer.SubmitOffer;
using Aizen.Modules.ServiceRequest.Application.Query.Offer.GetOfferCommissionPreview;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.ServiceRequest.Controller.V1.Offer;

[ApiController]
[Route("api/v1/service-requests/{serviceRequestId:long}/offers")]
[Tags("ServiceRequest - Offers")]
[Authorize]
[DocumentationInfo("Offer endpoints", "Provider offer management for service requests.")]
public sealed class ServiceRequestOfferController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public ServiceRequestOfferController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateServiceRequestOfferResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CreateServiceRequestOfferResponse?>> Create(
        [FromRoute] long serviceRequestId, [FromBody] CreateServiceRequestOfferRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<CreateServiceRequestOfferResponse>(new CreateServiceRequestOfferCommand(serviceRequestId, req), ct);
        return SetResponse(result);
    }

    [HttpPut("{offerId:long}")]
    [ProducesResponseType(typeof(UpdateServiceRequestOfferResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<UpdateServiceRequestOfferResponse?>> Update(
        [FromRoute] long serviceRequestId, [FromRoute] long offerId, [FromBody] UpdateServiceRequestOfferRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<UpdateServiceRequestOfferResponse>(new UpdateServiceRequestOfferCommand(serviceRequestId, offerId, req), ct);
        return SetResponse(result);
    }

    [HttpPatch("{offerId:long}/accept")]
    [ProducesResponseType(typeof(AcceptServiceRequestOfferResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AcceptServiceRequestOfferResponse?>> Accept(
        [FromRoute] long serviceRequestId, [FromRoute] long offerId, [FromBody] AcceptServiceRequestOfferRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<AcceptServiceRequestOfferResponse>(new AcceptServiceRequestOfferCommand(serviceRequestId, req), ct);
        return SetResponse(result);
    }

    /// <summary>
    /// BE-S7 offer-builder commission preview: per-line resolved rate / commission / provider-net + transaction totals.
    /// Compute-on-demand (nothing persisted). Optional <paramref name="providerPlanId"/> so plan-tier rates resolve.
    /// </summary>
    [HttpGet("{offerId:long}/commission-preview")]
    [ProducesResponseType(typeof(ResolveLineCommissionsRemoteCallResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ResolveLineCommissionsRemoteCallResponse?>> CommissionPreview(
        [FromRoute] long serviceRequestId, [FromRoute] long offerId,
        [FromQuery] long? providerPlanId = null, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<ResolveLineCommissionsRemoteCallResponse>(
            new GetOfferCommissionPreviewQuery(offerId, providerPlanId), ct);
        return SetResponse(result);
    }

    [HttpPatch("{offerId:long}/reject")]
    [ProducesResponseType(typeof(RejectServiceRequestOfferResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<RejectServiceRequestOfferResponse?>> Reject(
        [FromRoute] long serviceRequestId, [FromRoute] long offerId, [FromBody] RejectServiceRequestOfferRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<RejectServiceRequestOfferResponse>(new RejectServiceRequestOfferCommand(serviceRequestId, req), ct);
        return SetResponse(result);
    }

    [HttpPatch("{offerId:long}/withdraw")]
    [ProducesResponseType(typeof(WithdrawServiceRequestOfferResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<WithdrawServiceRequestOfferResponse?>> Withdraw(
        [FromRoute] long serviceRequestId, [FromRoute] long offerId, [FromBody] WithdrawServiceRequestOfferRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<WithdrawServiceRequestOfferResponse>(new WithdrawServiceRequestOfferCommand(serviceRequestId, offerId, req), ct);
        return SetResponse(result);
    }

    /// <summary>
    /// Aggregate save: get-or-create the caller's Draft, replace items, recompute server-authoritative totals.
    /// Client-sent totals are ignored.
    /// </summary>
    [HttpPut("draft")]
    [ProducesResponseType(typeof(SaveOfferDraftResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<SaveOfferDraftResponse?>> SaveDraft(
        [FromRoute] long serviceRequestId, [FromBody] SaveOfferDraftRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<SaveOfferDraftResponse>(new SaveOfferDraftCommand(serviceRequestId, req), ct);
        return SetResponse(result);
    }

    /// <summary>
    /// Runs the calculation service on the inputs without persisting. Returns computed totals.
    /// </summary>
    [HttpPost("preview")]
    [ProducesResponseType(typeof(PreviewOfferResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<PreviewOfferResponse?>> Preview(
        [FromRoute] long serviceRequestId, [FromBody] SaveOfferDraftRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<PreviewOfferResponse>(new PreviewOfferCommand(serviceRequestId, req), ct);
        return SetResponse(result);
    }

    /// <summary>
    /// Draft → Submitted. Idempotent on idempotency key. Rejects empty offers.
    /// </summary>
    [HttpPatch("{offerId:long}/submit")]
    [ProducesResponseType(typeof(SubmitOfferResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<SubmitOfferResponse?>> Submit(
        [FromRoute] long serviceRequestId, [FromRoute] long offerId, [FromBody] SubmitOfferRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<SubmitOfferResponse>(new SubmitOfferCommand(serviceRequestId, offerId, req), ct);
        return SetResponse(result);
    }
}
