using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ServiceRequest.Abstraction.Dto.Pricing;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Pricing;
using Aizen.Modules.ServiceRequest.Application.Command.Pricing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.ServiceRequest.Controller.V1.Admin;

/// <summary>S2a — admin CRUD over pricing attribute definitions (category-scoped, R4-lookup-backed).</summary>
[ApiController]
[Route("api/v1/admin/service-requests/pricing-attributes")]
[Tags("Admin - ServiceRequest Pricing Attributes")]
[Authorize(Roles = "Admin")]
public sealed class AdminPricingAttributeController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public AdminPricingAttributeController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor) => _cqrs = cqrs;

    [HttpGet]
    public async Task<AizenApiResponse<List<PricingAttributeDefinitionDto>?>> List(
        [FromQuery] string? serviceCategoryCode = null, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<List<PricingAttributeDefinitionDto>>(
            new ListPricingAttributeDefinitionsQuery(serviceCategoryCode), ct);
        return SetResponse(result);
    }

    [HttpPost]
    public async Task<AizenApiResponse<PricingAttributeDefinitionDto?>> Create(
        [FromBody] PricingAttributeDefinitionRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<PricingAttributeDefinitionDto>(
            new CreatePricingAttributeDefinitionCommand(req), ct);
        return SetResponse(result);
    }

    [HttpPut("{id:long}")]
    public async Task<AizenApiResponse<PricingAttributeDefinitionDto?>> Update(
        [FromRoute] long id, [FromBody] PricingAttributeDefinitionRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<PricingAttributeDefinitionDto>(
            new UpdatePricingAttributeDefinitionCommand(id, req), ct);
        return SetResponse(result);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete([FromRoute] long id, CancellationToken ct = default)
    {
        await _cqrs.ProcessAsync<bool>(new DeletePricingAttributeDefinitionCommand(id), ct);
        return Ok(new { Header = new { IsSuccess = true } });
    }
}
