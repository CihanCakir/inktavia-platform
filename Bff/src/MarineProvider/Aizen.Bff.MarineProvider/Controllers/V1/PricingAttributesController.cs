using Aizen.Bff.MarineProvider.Application.Common.Authorization;
using Aizen.Bff.MarineProvider.Application.PricingAttributes;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ServiceRequest.Abstraction.Dto.Pricing;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Pricing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.MarineProvider.Controllers.V1;

/// <summary>
/// BE-S2 — provider offer-line pricing attributes: the applicable definitions (with resolved Lookup options) for an SR's
/// category, and get/set of the values on an offer line. Thin passthrough to the ServiceRequest module.
/// </summary>
[ApiController]
[Route("api/v1/provider")]
[Tags("Provider - Pricing Attributes")]
[Authorize(Policy = ProviderAuthorizationPolicies.ProviderActive)]
public sealed class PricingAttributesController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public PricingAttributesController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor) => _cqrs = cqrs;

    /// <summary>The pricing attributes applicable to the SR's category, with resolved Lookup options.</summary>
    [HttpGet("service-requests/{serviceRequestId:long}/pricing-attributes")]
    [ProducesResponseType(typeof(List<ApplicablePricingAttributeDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<List<ApplicablePricingAttributeDto>?>> GetApplicable(
        long serviceRequestId, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new GetApplicablePricingAttributesBffQuery { ServiceRequestId = serviceRequestId }, ct));

    /// <summary>Current pricing attribute values on one offer line.</summary>
    [HttpGet("offers/{offerId:long}/items/{itemId:long}/attributes")]
    [ProducesResponseType(typeof(List<PricingAttributeValueDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<List<PricingAttributeValueDto>?>> GetLineAttributes(
        long offerId, long itemId, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new GetOfferLineAttributesBffQuery { OfferId = offerId, ItemId = itemId }, ct));

    /// <summary>Set (full-replace) the pricing attribute values on one offer line.</summary>
    [HttpPut("offers/{offerId:long}/items/{itemId:long}/attributes")]
    [ProducesResponseType(typeof(List<PricingAttributeValueDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<List<PricingAttributeValueDto>?>> SetLineAttributes(
        long offerId, long itemId, [FromBody] SetOfferLineAttributesRequest body, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new SetOfferLineAttributesBffCommand { OfferId = offerId, ItemId = itemId, Body = body }, ct));
}
