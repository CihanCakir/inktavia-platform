using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ServiceRequest.Abstraction.Dto.Pricing;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Pricing;
using Aizen.Modules.ServiceRequest.Application.Command.Pricing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.ServiceRequest.Controller.V1.Pricing;

/// <summary>S2b/S2c — provider reads the applicable pricing attributes for an SR and sets values per offer line.</summary>
[ApiController]
[Route("api/v1/service-requests/provider")]
[Tags("ServiceRequest - Provider Pricing Attributes")]
[Authorize]
public sealed class ProviderPricingAttributeController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public ProviderPricingAttributeController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor) => _cqrs = cqrs;

    /// <summary>The pricing attributes applicable to the SR's category, with resolved Lookup options.</summary>
    [HttpGet("service-requests/{serviceRequestId:long}/applicable-pricing-attributes")]
    public async Task<AizenApiResponse<List<ApplicablePricingAttributeDto>?>> Applicable(
        [FromRoute] long serviceRequestId, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<List<ApplicablePricingAttributeDto>>(
            new GetApplicablePricingAttributesQuery(serviceRequestId), ct);
        return SetResponse(result);
    }

    /// <summary>Current pricing attribute values on one offer line.</summary>
    [HttpGet("offers/{offerId:long}/items/{itemId:long}/attributes")]
    public async Task<AizenApiResponse<List<PricingAttributeValueDto>?>> Get(
        [FromRoute] long offerId, [FromRoute] long itemId, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<List<PricingAttributeValueDto>>(new GetOfferLineAttributesQuery(itemId), ct);
        return SetResponse(result);
    }

    /// <summary>Set (full-replace) the pricing attribute values on one offer line.</summary>
    [HttpPut("offers/{offerId:long}/items/{itemId:long}/attributes")]
    public async Task<AizenApiResponse<List<PricingAttributeValueDto>?>> Set(
        [FromRoute] long offerId, [FromRoute] long itemId,
        [FromBody] SetOfferLineAttributesRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<List<PricingAttributeValueDto>>(
            new SetOfferLineAttributesCommand(offerId, itemId, req), ct);
        return SetResponse(result);
    }
}
