using Aizen.Bff.MarineProvider.Application.Common.Authorization;
using Aizen.Bff.MarineProvider.Application.TravelPricing;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ServiceRequest.Abstraction.Dto.Travel;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Travel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.MarineProvider.Controllers.V1;

/// <summary>
/// BE-S4 — provider offer-line travel pricing: get/set the structured TravelPricingDetail (FlatMobilization / PerKm,
/// origin+destination city, distanceKm, perKmRate, unit) on a Travel line. Thin passthrough to the ServiceRequest module;
/// the server validates the derivation against the line's own money and surfaces SR_TRAVEL_* verbatim.
/// </summary>
[ApiController]
[Route("api/v1/provider")]
[Tags("Provider - Travel Pricing")]
[Authorize(Policy = ProviderAuthorizationPolicies.ProviderActive)]
public sealed class TravelPricingController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public TravelPricingController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor) => _cqrs = cqrs;

    /// <summary>The current travel-pricing detail on one offer line (null when none is set).</summary>
    [HttpGet("offers/{offerId:long}/items/{itemId:long}/travel-pricing")]
    [ProducesResponseType(typeof(TravelPricingDetailDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<TravelPricingDetailDto?>> GetTravelPricing(
        long offerId, long itemId, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new GetOfferLineTravelPricingBffQuery { OfferId = offerId, ItemId = itemId }, ct));

    /// <summary>Set (upsert) the travel-pricing detail on one Travel offer line.</summary>
    [HttpPut("offers/{offerId:long}/items/{itemId:long}/travel-pricing")]
    [ProducesResponseType(typeof(TravelPricingDetailDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<TravelPricingDetailDto?>> SetTravelPricing(
        long offerId, long itemId, [FromBody] SetOfferLineTravelPricingRequest body, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new SetOfferLineTravelPricingBffCommand { OfferId = offerId, ItemId = itemId, Body = body }, ct));
}
