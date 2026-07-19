using Aizen.Bff.MarineProvider.Application.Catalog;
using Aizen.Bff.MarineProvider.Application.Common;
using Aizen.Bff.MarineProvider.Application.Common.Authorization;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ServiceRequest.Abstraction.Dto;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Offer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.MarineProvider.Controllers.V1;

[ApiController]
[Route("api/v1/provider/offer-catalog")]
[Tags("Provider - Offer Catalog")]
[Authorize(Policy = ProviderAuthorizationPolicies.ProviderActive)]
public sealed class CatalogController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public CatalogController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor) => _cqrs = cqrs;

    [HttpGet]
    [ProducesResponseType(typeof(List<ProviderCatalogItemDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<List<ProviderCatalogItemDto>?>> List(CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new ListOfferCatalogBffQuery(), ct));

    [HttpPost]
    [ProducesResponseType(typeof(ProviderCatalogItemDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProviderCatalogItemDto?>> Create(
        [FromBody] CatalogItemRequest body, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new CreateOfferCatalogItemBffCommand { Body = body }, ct));

    [HttpPut("{id:long}")]
    [ProducesResponseType(typeof(ProviderCatalogItemDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProviderCatalogItemDto?>> Update(
        [FromRoute] long id, [FromBody] CatalogItemRequest body, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new UpdateOfferCatalogItemBffCommand { Id = id, Body = body }, ct));

    [HttpDelete("{id:long}")]
    [ProducesResponseType(typeof(BffSuccessResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<BffSuccessResult?>> Delete(
        [FromRoute] long id, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new DeleteOfferCatalogItemBffCommand { Id = id }, ct));
}
