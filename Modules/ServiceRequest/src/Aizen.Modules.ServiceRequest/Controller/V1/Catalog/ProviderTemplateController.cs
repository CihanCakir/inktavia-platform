using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ServiceRequest.Abstraction.Dto;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Offer;
using Aizen.Modules.ServiceRequest.Application.Command.Catalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.ServiceRequest.Controller.V1.Catalog;

[ApiController]
[Route("api/v1/service-requests/provider/templates")]
[Tags("ServiceRequest - Provider Templates")]
[Authorize]
public sealed class ProviderTemplateController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public ProviderTemplateController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor) => _cqrs = cqrs;

    [HttpGet]
    public async Task<AizenApiResponse<List<ProviderOfferTemplateDto>?>> List(CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<List<ProviderOfferTemplateDto>>(new ListTemplatesQuery(), ct);
        return SetResponse(result);
    }

    [HttpGet("{id:long}")]
    public async Task<AizenApiResponse<ProviderOfferTemplateDto?>> Get([FromRoute] long id, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<ProviderOfferTemplateDto>(new GetTemplateQuery(id), ct);
        return SetResponse(result);
    }

    [HttpPost]
    public async Task<AizenApiResponse<ProviderOfferTemplateDto?>> Create([FromBody] OfferTemplateRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<ProviderOfferTemplateDto>(new CreateTemplateCommand(req), ct);
        return SetResponse(result);
    }

    [HttpPut("{id:long}")]
    public async Task<AizenApiResponse<ProviderOfferTemplateDto?>> Update([FromRoute] long id, [FromBody] OfferTemplateRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<ProviderOfferTemplateDto>(new UpdateTemplateCommand(id, req), ct);
        return SetResponse(result);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete([FromRoute] long id, CancellationToken ct = default)
    {
        await _cqrs.ProcessAsync<bool>(new DeleteTemplateCommand(id), ct);
        return Ok(new { Header = new { IsSuccess = true } });
    }
}
