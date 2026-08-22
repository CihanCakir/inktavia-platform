using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Currency;
using Aizen.Modules.ReferenceData.Abstraction.Request.Currency;
using Aizen.Modules.ReferenceData.Application.Currency.Commands;
using Aizen.Modules.ReferenceData.Application.Currency.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.ReferenceData.Controller.V1.ReferenceData;

// Salt-okunur para birimi sorguları. Tek çağıran AdminPanel BFF'tir ve servis token'ını iletir; token'sız
// çağıran modül→modül yol YOKTUR. Bu yüzden örtük FallbackPolicy'ye güvenmek yerine niyeti AÇIK yazıyoruz:
// kimlik doğrulanmış erişim. (ExchangeRate/Lookup'tan farkı: onları token'sız bir modül çağırıyordu, bunu
// değil.) Bir gün token'sız bir modül çağıranı olursa bilinçli bir karar gerekir; sessizce 401 olmasın diye
// [Authorize] burada açıkça duruyor.
[ApiController]
[Authorize]
[Route("api/v1/reference-data/currencies")]
[Tags("Currency")]
[DocumentationInfo("Currency read endpoints", "Read-only currency queries available to all authenticated users.")]
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
}
