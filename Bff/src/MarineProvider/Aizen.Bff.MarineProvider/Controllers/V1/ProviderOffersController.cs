using Aizen.Bff.MarineProvider.Application.Common.Authorization;
using Aizen.Bff.MarineProvider.Application.Offers;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Offer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.MarineProvider.Controllers.V1;

[ApiController]
[Route("api/v1/provider")]
[Tags("Provider - Offers")]
[Authorize(Policy = ProviderAuthorizationPolicies.ProviderActive)]
public sealed class ProviderOffersController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public ProviderOffersController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    [HttpGet("offers")]
    public async Task<IActionResult> GetMyOffers(
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? status = null,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new GetMyOffersBffQuery
        {
            PageIndex = pageIndex,
            PageSize = pageSize,
            Status = status,
        }, ct);
        return Ok(SetResponse(result));
    }

    [HttpPost("service-requests/{serviceRequestId:long}/offers")]
    public async Task<IActionResult> CreateOffer(
        long serviceRequestId,
        [FromBody] CreateServiceRequestOfferRequest body,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new CreateOfferBffCommand
        {
            ServiceRequestId = serviceRequestId,
            Body = body,
        }, ct);
        return Ok(SetResponse(result));
    }

    [HttpPut("service-requests/{serviceRequestId:long}/offers/{offerId:long}")]
    public async Task<IActionResult> UpdateOffer(
        long serviceRequestId, long offerId,
        [FromBody] UpdateServiceRequestOfferRequest body,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new UpdateOfferBffCommand
        {
            ServiceRequestId = serviceRequestId,
            OfferId = offerId,
            Body = body,
        }, ct);
        return Ok(SetResponse(result));
    }

    [HttpPost("offers/{offerId:long}/withdraw")]
    public async Task<IActionResult> WithdrawOffer(
        long offerId,
        [FromBody] WithdrawOfferBffRequest body,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new WithdrawOfferBffCommand
        {
            ServiceRequestId = body.ServiceRequestId,
            OfferId = offerId,
            Reason = body.Reason,
        }, ct);
        return Ok(SetResponse(result));
    }
}

public sealed class WithdrawOfferBffRequest
{
    public long ServiceRequestId { get; set; }
    public string? Reason { get; set; }
}
