using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ReferenceData.Abstraction.Dto.ExchangeRate;
using Aizen.Modules.ReferenceData.Abstraction.Model;
using Aizen.Modules.ReferenceData.Application.ExchangeRate.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.ReferenceData.Controller.V1.ReferenceData;

[ApiController]
[Route("api/v1/reference-data/exchange-rates")]
[Tags("ExchangeRate")]
[DocumentationInfo("Exchange rate read endpoints", "Read-only queries for current and historical exchange rates.")]
public sealed class ExchangeRateController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public ExchangeRateController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ExchangeRateDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ExchangeRateDto?>> GetRate([FromQuery] string fromCurrencyCode, [FromQuery] string toCurrencyCode, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<ExchangeRateDto?>(new GetExchangeRateQuery(fromCurrencyCode, toCurrencyCode), ct);
        return SetResponse(result);
    }

    [HttpGet("by-currency/{currencyCode}")]
    [ProducesResponseType(typeof(IReadOnlyList<ExchangeRateDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<IReadOnlyList<ExchangeRateDto>>> GetByCurrency([FromRoute] string currencyCode, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<IReadOnlyList<ExchangeRateDto>>(new GetExchangeRatesByCurrencyQuery(currencyCode), ct);
        return SetResponse(result);
    }

    [HttpGet("history")]
    [ProducesResponseType(typeof(IReadOnlyList<ExchangeRateHistoryDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<IReadOnlyList<ExchangeRateHistoryDto>>> GetHistory([FromQuery] string fromCurrencyCode, [FromQuery] string toCurrencyCode, [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<IReadOnlyList<ExchangeRateHistoryDto>>(new GetExchangeRateHistoryQuery(fromCurrencyCode, toCurrencyCode, startDate, endDate), ct);
        return SetResponse(result);
    }
}
