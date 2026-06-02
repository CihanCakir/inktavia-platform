using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Currency;
using Aizen.Modules.ReferenceData.Abstraction.Model;
using Aizen.Modules.ReferenceData.Abstraction.Request.Currency;
using Aizen.Modules.ReferenceData.Application.Currency.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.ReferenceData.Controller.V1.Admin;

[ApiController]
[Route("api/v1/admin/reference-data/currencies")]
[Tags("Admin - Currency")]
[Authorize(Roles = "Admin")]
[DocumentationInfo("Currency admin endpoints", "Create, update and lifecycle management of currencies. Requires Admin role.")]
public sealed class CurrencyAdminController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public CurrencyAdminController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CurrencyDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CurrencyDto?>> CreateAsync([FromBody] CreateCurrencyRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<CurrencyDto>(new CreateCurrencyCommand(req), ct);
        return SetResponse(result);
    }

    [HttpPut("{id:long}")]
    [ProducesResponseType(typeof(CurrencyDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CurrencyDto?>> UpdateAsync([FromRoute] long id, [FromBody] UpdateCurrencyRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<CurrencyDto>(new UpdateCurrencyCommand(id, req.Name, req.Symbol, req.DecimalPlaces, req.IsActive), ct);
        return SetResponse(result);
    }

    [HttpPut("{id:long}/set-base")]
    [ProducesResponseType(typeof(CurrencyDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CurrencyDto?>> SetBaseAsync([FromRoute] long id, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<CurrencyDto>(new SetBaseCurrencyCommand(id), ct);
        return SetResponse(result);
    }

    [HttpPut("{id:long}/activate")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<object>> ActivateAsync([FromRoute] long id, CancellationToken ct = default)
    {
        await _cqrs.ProcessAsync<bool>(new ActivateCurrencyCommand(id), ct);
        return SetResponse<object>(new { success = true });
    }

    [HttpPut("{id:long}/deactivate")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<object>> DeactivateAsync([FromRoute] long id, CancellationToken ct = default)
    {
        await _cqrs.ProcessAsync<bool>(new DeactivateCurrencyCommand(id), ct);
        return SetResponse<object>(new { success = true });
    }
}
