using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ReferenceData.Abstraction.Dto.ExchangeRate;
using Aizen.Modules.ReferenceData.Application.ExchangeRate.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.ReferenceData.Controller.V1.ReferenceData;

// ⚠️ SINIF DÜZEYİ [AllowAnonymous] — SALT-OKUNUR CONTROLLER. Buraya yazma ucu (POST/PUT/DELETE) EKLEME.
// Kur (exchange-rate) listeleri public referans veridir. ServiceRequest modülü teklif FX'ini token'sız
// çağırır (IServiceRequestReferenceDataRemoteCall Authorization başlığı iletmez). Attribute yokken
// AddAizenKeycloakAuth'un global FallbackPolicy'si bu GET'leri de auth'a zorluyor ve modül→modül çağrıyı
// 401'e düşürüyordu. "Anonymous" = "cluster İÇİNDE token gerekmez"; modüllerin public ingress'i yoktur ve
// NetworkPolicy yalnız BFF'leri geçirir — açık olan bu sınır. Kur mutasyonları ayrı, yetkili bir admin
// controller'ına ait, buraya değil.
[ApiController]
[AllowAnonymous]
[Route("api/v1/reference-data/exchange-rates")]
[Tags("ExchangeRate")]
[DocumentationInfo("Exchange rate read endpoints", "Read-only queries for current and historical exchange rates. Public reference data — no auth required inside the cluster.")]
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

    [HttpGet("resolve")]
    [ProducesResponseType(typeof(ExchangeRateResolveDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ExchangeRateResolveDto>> Resolve([FromQuery] string fromCurrencyCode, [FromQuery] string toCurrencyCode, [FromQuery] DateTimeOffset asOfUtc, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<ExchangeRateResolveDto>(new ResolveExchangeRateQuery(fromCurrencyCode, toCurrencyCode, asOfUtc), ct);
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
