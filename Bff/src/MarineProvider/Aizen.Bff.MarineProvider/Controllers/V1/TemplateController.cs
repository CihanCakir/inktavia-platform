using Aizen.Bff.MarineProvider.Application.Common;
using Aizen.Bff.MarineProvider.Application.Common.Authorization;
using Aizen.Bff.MarineProvider.Application.Template;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ServiceRequest.Abstraction.Dto;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Offer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.MarineProvider.Controllers.V1;

[ApiController]
[Route("api/v1/provider/offer-templates")]
[Tags("Provider - Offer Templates")]
[Authorize(Policy = ProviderAuthorizationPolicies.ProviderActive)]
public sealed class TemplateController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public TemplateController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor) => _cqrs = cqrs;

    [HttpGet]
    [ProducesResponseType(typeof(List<ProviderOfferTemplateDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<List<ProviderOfferTemplateDto>?>> List(CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new ListOfferTemplatesBffQuery(), ct));

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ProviderOfferTemplateDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProviderOfferTemplateDto?>> Get(
        [FromRoute] long id, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new GetOfferTemplateBffQuery { Id = id }, ct));

    [HttpPost]
    [ProducesResponseType(typeof(ProviderOfferTemplateDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProviderOfferTemplateDto?>> Create(
        [FromBody] OfferTemplateRequest body, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new CreateOfferTemplateBffCommand { Body = body }, ct));

    [HttpPut("{id:long}")]
    [ProducesResponseType(typeof(ProviderOfferTemplateDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProviderOfferTemplateDto?>> Update(
        [FromRoute] long id, [FromBody] OfferTemplateRequest body, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new UpdateOfferTemplateBffCommand { Id = id, Body = body }, ct));

    [HttpDelete("{id:long}")]
    [ProducesResponseType(typeof(BffSuccessResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<BffSuccessResult?>> Delete(
        [FromRoute] long id, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new DeleteOfferTemplateBffCommand { Id = id }, ct));
}
