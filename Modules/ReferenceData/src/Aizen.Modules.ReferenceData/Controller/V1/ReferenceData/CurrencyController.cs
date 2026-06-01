using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Currency;
using Aizen.Modules.ReferenceData.Abstraction.Model;
using Aizen.Modules.ReferenceData.Abstraction.Request.Currency;
using Aizen.Modules.ReferenceData.Application.Currency.Commands;
using Aizen.Modules.ReferenceData.Application.Currency.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.ReferenceData.Controller.V1.ReferenceData;

[ApiController]
[Route("api/v1/reference-data/currencies")]
[Tags("Currency")]
[DocumentationInfo("Currency management endpoints", "CRUD and lifecycle operations for currencies.")]
public sealed class CurrencyController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public CurrencyController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CurrencyDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<IReadOnlyList<CurrencyDto>>> GetList([FromQuery] bool onlyActive = true, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<IReadOnlyList<CurrencyDto>>(new GetCurrencyListQuery(onlyActive), ct);
        return SetResponse(result);
    }

    [HttpGet("base")]
    [ProducesResponseType(typeof(CurrencyDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CurrencyDto?>> GetBase(CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<CurrencyDto?>(new GetBaseCurrencyQuery(), ct);
        return SetResponse(result);
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(CurrencyDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CurrencyDto?>> GetById([FromRoute] long id, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<CurrencyDto?>(new GetCurrencyDetailQuery(id), ct);
        return SetResponse(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CurrencyDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CurrencyDto?>> Create([FromBody] CreateCurrencyRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<CurrencyDto>(new CreateCurrencyCommand(req), ct);
        return SetResponse(result);
    }

    [HttpPut("{id:long}")]
    [ProducesResponseType(typeof(CurrencyDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CurrencyDto?>> Update([FromRoute] long id, [FromBody] UpdateCurrencyRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<CurrencyDto>(new UpdateCurrencyCommand(id, req.Name, req.Symbol, req.DecimalPlaces, req.IsActive), ct);
        return SetResponse(result);
    }

    [HttpPut("{id:long}/set-base")]
    [ProducesResponseType(typeof(CurrencyDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CurrencyDto?>> SetBase([FromRoute] long id, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<CurrencyDto>(new SetBaseCurrencyCommand(id), ct);
        return SetResponse(result);
    }

    [HttpPut("{id:long}/activate")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<object>> Activate([FromRoute] long id, CancellationToken ct = default)
    {
        await _cqrs.ProcessAsync<bool>(new ActivateCurrencyCommand(id), ct);
        return SetResponse<object>(new { success = true });
    }

    [HttpPut("{id:long}/deactivate")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<object>> Deactivate([FromRoute] long id, CancellationToken ct = default)
    {
        await _cqrs.ProcessAsync<bool>(new DeactivateCurrencyCommand(id), ct);
        return SetResponse<object>(new { success = true });
    }
}
