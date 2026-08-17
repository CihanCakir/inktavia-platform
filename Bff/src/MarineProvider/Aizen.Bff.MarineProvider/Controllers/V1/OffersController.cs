using Aizen.Bff.MarineProvider.Application.Common.Authorization;
using Aizen.Bff.MarineProvider.Application.Offers;
using Aizen.Bff.MarineProvider.Application.Offers.Contracts;
using Aizen.Bff.MarineProvider.Application.PartTerms;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Responses;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Offer;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Offer;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Provider;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.MarineProvider.Controllers.V1;

[ApiController]
[Route("api/v1/provider")]
[Tags("Provider - Offers")]
[Authorize(Policy = ProviderAuthorizationPolicies.ProviderActive)]
public sealed class OffersController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public OffersController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor) => _cqrs = cqrs;

    [HttpGet("offers")]
    [ProducesResponseType(typeof(GetMyOffersResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetMyOffersResponse?>> GetMyOffers(
        [FromQuery] int pageIndex = 0, [FromQuery] int pageSize = 20, [FromQuery] int? status = null,
        CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new GetMyOffersBffQuery { PageIndex = pageIndex, PageSize = pageSize, Status = status }, ct));

    [HttpPost("service-requests/{serviceRequestId:long}/offers")]
    [ProducesResponseType(typeof(CreateOfferBffResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CreateOfferBffResponse?>> CreateOffer(
        long serviceRequestId, [FromBody] CreateServiceRequestOfferRequest body, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new CreateOfferBffCommand { ServiceRequestId = serviceRequestId, Body = body }, ct));

    [HttpPut("service-requests/{serviceRequestId:long}/offers/{offerId:long}")]
    [ProducesResponseType(typeof(UpdateOfferBffResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<UpdateOfferBffResponse?>> UpdateOffer(
        long serviceRequestId, long offerId, [FromBody] UpdateServiceRequestOfferRequest body, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new UpdateOfferBffCommand { ServiceRequestId = serviceRequestId, OfferId = offerId, Body = body }, ct));

    [HttpPost("offers/{offerId:long}/withdraw")]
    [ProducesResponseType(typeof(WithdrawOfferBffResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<WithdrawOfferBffResponse?>> WithdrawOffer(
        long offerId, [FromBody] WithdrawServiceRequestOfferRequest body, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new WithdrawOfferBffCommand { ServiceRequestId = body.ServiceRequestId, OfferId = offerId, Reason = body.Reason }, ct));

    /// <summary>Aggregate draft save. Server computes all totals — BFF passes through untouched.</summary>
    [HttpPut("service-requests/{serviceRequestId:long}/offer/draft")]
    [ProducesResponseType(typeof(SaveOfferDraftResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<SaveOfferDraftResponse?>> SaveOfferDraft(
        long serviceRequestId, [FromBody] SaveOfferDraftRequest body, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new SaveOfferDraftBffCommand { ServiceRequestId = serviceRequestId, Body = body }, ct));

    /// <summary>Preview: runs calculation without persisting.</summary>
    [HttpPost("service-requests/{serviceRequestId:long}/offer/preview")]
    [ProducesResponseType(typeof(PreviewOfferResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<PreviewOfferResponse?>> PreviewOffer(
        long serviceRequestId, [FromBody] SaveOfferDraftRequest body, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new PreviewOfferBffCommand { ServiceRequestId = serviceRequestId, Body = body }, ct));

    /// <summary>
    /// BE-S7 offer-builder commission preview: per-line resolved rate/commission/provider-net + transaction totals.
    /// Compute-on-demand (nothing persisted); provider resolved by-subject. Optional providerPlanId for plan-tier rates.
    /// </summary>
    [HttpPost("offers/commission-preview")]
    [ProducesResponseType(typeof(ResolveLineCommissionsRemoteCallResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ResolveLineCommissionsRemoteCallResponse?>> GetOfferCommissionPreview(
        [FromBody] OfferCommissionPreviewBffRequest body, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new GetOfferCommissionPreviewBffQuery
        {
            ProviderPlanId      = body.ProviderPlanId,
            CurrencyCode        = body.CurrencyCode,
            OfferDiscountAmount = body.OfferDiscountAmount,
            Lines               = body.Lines,
        }, ct));

    /// <summary>
    /// BE-S6 offer-builder customer-discount preview: resolved rule + requested discount + funding split.
    /// BFF computes EligibleServiceBaseAmount from raw line inputs (server owns totals).
    /// </summary>
    [HttpPost("offers/customer-discount-preview")]
    [ProducesResponseType(typeof(ResolveCustomerDiscountRemoteCallResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ResolveCustomerDiscountRemoteCallResponse?>> GetOfferCustomerDiscountPreview(
        [FromBody] OfferCustomerDiscountPreviewBffRequest body, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new GetOfferCustomerDiscountPreviewBffQuery
        {
            CustomerPlanId      = body.CustomerPlanId,
            CategoryCode        = body.CategoryCode,
            CurrencyCode        = body.CurrencyCode,
            OfferDiscountAmount = body.OfferDiscountAmount,
            Lines               = body.Lines,
        }, ct));

    /// <summary>
    /// BE-S5c offer-builder part-terms preview: the cost-free allowance (max customer discount + funded split +
    /// min-receivable) per Product/Consumable line. Compute-on-demand (nothing persisted). Never returns supplier cost /
    /// dealer margin — only the derived caps (§20.9 confidentiality).
    /// </summary>
    [HttpGet("service-requests/{serviceRequestId:long}/offers/{offerId:long}/part-terms-preview")]
    [ProducesResponseType(typeof(ResolvePartLineAllowancesRemoteCallResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ResolvePartLineAllowancesRemoteCallResponse?>> GetOfferPartTermsPreview(
        long serviceRequestId, long offerId, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new GetOfferPartTermsPreviewBffQuery { ServiceRequestId = serviceRequestId, OfferId = offerId }, ct));

    /// <summary>Draft -> Submitted. Idempotent on idempotency key.</summary>
    [HttpPost("service-requests/{serviceRequestId:long}/offer/{offerId:long}/submit")]
    [ProducesResponseType(typeof(SubmitOfferResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<SubmitOfferResponse?>> SubmitOffer(
        long serviceRequestId, long offerId, [FromBody] SubmitOfferRequest body, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new SubmitOfferBffCommand { ServiceRequestId = serviceRequestId, OfferId = offerId, Body = body }, ct));

    /// <summary>Withdraw an offer (keeps the row for history).</summary>
    [HttpPost("service-requests/{serviceRequestId:long}/offer/{offerId:long}/withdraw")]
    [ProducesResponseType(typeof(WithdrawOfferBffResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<WithdrawOfferBffResponse?>> WithdrawOfferNew(
        long serviceRequestId, long offerId, [FromBody] WithdrawServiceRequestOfferRequest body, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new WithdrawOfferBffCommand { ServiceRequestId = serviceRequestId, OfferId = offerId, Reason = body.Reason }, ct));
}
