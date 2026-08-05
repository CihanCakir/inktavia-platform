using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ServiceRequest.Abstraction.Dto.Travel;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Travel;
using Aizen.Modules.ServiceRequest.Application.Command.Travel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.ServiceRequest.Controller.V1.Travel;

/// <summary>BE-S4a — provider reads/sets the structured travel-pricing detail on a Travel offer line.</summary>
[ApiController]
[Route("api/v1/service-requests/provider")]
[Tags("ServiceRequest - Provider Travel Pricing")]
[Authorize]
public sealed class ProviderTravelPricingController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public ProviderTravelPricingController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor) => _cqrs = cqrs;

    /// <summary>The current travel-pricing detail on one offer line (null when none is set).</summary>
    [HttpGet("offers/{offerId:long}/items/{itemId:long}/travel-pricing")]
    public async Task<AizenApiResponse<TravelPricingDetailDto?>> Get(
        [FromRoute] long offerId, [FromRoute] long itemId, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<TravelPricingDetailDto>(new GetOfferLineTravelPricingQuery(itemId), ct);
        return SetResponse(result);
    }

    /// <summary>Set (upsert) the travel-pricing detail on one Travel offer line.</summary>
    [HttpPut("offers/{offerId:long}/items/{itemId:long}/travel-pricing")]
    public async Task<AizenApiResponse<TravelPricingDetailDto?>> Set(
        [FromRoute] long offerId, [FromRoute] long itemId,
        [FromBody] SetOfferLineTravelPricingRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<TravelPricingDetailDto>(
            new SetOfferLineTravelPricingCommand(offerId, itemId, req), ct);
        return SetResponse(result);
    }
}
