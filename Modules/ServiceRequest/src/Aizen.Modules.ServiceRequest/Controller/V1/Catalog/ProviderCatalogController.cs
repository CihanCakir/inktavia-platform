using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ServiceRequest.Abstraction.Dto;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Offer;
using Aizen.Modules.ServiceRequest.Application.Command.Catalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.ServiceRequest.Controller.V1.Catalog;

[ApiController]
[Route("api/v1/service-requests/provider/catalog")]
[Tags("ServiceRequest - Provider Catalog")]
[Authorize]
public sealed class ProviderCatalogController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public ProviderCatalogController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor) => _cqrs = cqrs;

    [HttpGet]
    public async Task<AizenApiResponse<List<ProviderCatalogItemDto>?>> List(CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<List<ProviderCatalogItemDto>>(new ListCatalogItemsQuery(), ct);
        return SetResponse(result);
    }

    [HttpPost]
    public async Task<AizenApiResponse<ProviderCatalogItemDto?>> Create([FromBody] CatalogItemRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<ProviderCatalogItemDto>(new CreateCatalogItemCommand(req), ct);
        return SetResponse(result);
    }

    [HttpPut("{id:long}")]
    public async Task<AizenApiResponse<ProviderCatalogItemDto?>> Update([FromRoute] long id, [FromBody] CatalogItemRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<ProviderCatalogItemDto>(new UpdateCatalogItemCommand(id, req), ct);
        return SetResponse(result);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete([FromRoute] long id, CancellationToken ct = default)
    {
        await _cqrs.ProcessAsync<bool>(new DeleteCatalogItemCommand(id), ct);
        return Ok(new { Header = new { IsSuccess = true } });
    }
}
