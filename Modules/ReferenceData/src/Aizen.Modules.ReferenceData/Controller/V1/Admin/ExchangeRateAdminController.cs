using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ReferenceData.Abstraction.Dto.ExchangeRate;
using Aizen.Modules.ReferenceData.Abstraction.Request.ExchangeRate;
using Aizen.Modules.ReferenceData.Application.ExchangeRate.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.ReferenceData.Controller.V1.Admin;

[ApiController]
[Route("api/v1/admin/reference-data/exchange-rates")]
[Tags("Admin - ExchangeRate")]
[Authorize(Roles = "Admin")]
[DocumentationInfo("Exchange rate admin endpoints", "Update exchange rates and sync bulk rates. Requires Admin role.")]
public sealed class ExchangeRateAdminController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public ExchangeRateAdminController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    [HttpPut]
    [ProducesResponseType(typeof(ExchangeRateDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ExchangeRateDto?>> Update([FromBody] UpdateExchangeRateRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<ExchangeRateDto>(new UpdateExchangeRateCommand(req), ct);
        return SetResponse(result);
    }

    [HttpPost("sync")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<object>> Sync([FromBody] IEnumerable<UpdateExchangeRateRequest> requests, CancellationToken ct = default)
    {
        await _cqrs.ProcessAsync<bool>(new SyncExchangeRatesCommand(requests), ct);
        return SetResponse<object>(new { success = true });
    }

    [HttpPost("history")]
    [ProducesResponseType(typeof(ExchangeRateHistoryDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ExchangeRateHistoryDto?>> CreateHistory([FromBody] UpdateExchangeRateRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<ExchangeRateHistoryDto>(new CreateExchangeRateHistoryCommand(req.FromCurrencyCode, req.ToCurrencyCode, req.Rate, req.ProviderType, req.RateDate, req.RawProviderPayload), ct);
        return SetResponse(result);
    }
}
