using Aizen.Bff.MarineProvider.Application.Common.Authorization;
using Aizen.Bff.MarineProvider.Application.Location;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.MarineProvider.Controllers.V1;

[ApiController]
[Route("api/v1/provider/location")]
[Tags("Provider - Location")]
[Authorize(Policy = ProviderAuthorizationPolicies.ProviderAuthenticated)]
public sealed class LocationController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public LocationController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor) => _cqrs = cqrs;

    [HttpGet("cities")]
    [ProducesResponseType(typeof(List<CityDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<List<CityDto>?>> GetCities(
        [FromQuery] string country = "TR", CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new GetCitiesBffQuery { Country = country }, ct));

    // FAZ17 (#72) — ReferenceData "countries" ucu baştan beri vardı; eksik olan bu passthrough'tu (ölçüldü 2026-08-22).
    // Şehirlerle aynı desen. Pilot (yalnız Türkiye) filtresi BURADA DEĞİL — BFF, ReferenceData'da ne varsa sunar;
    // pilot kısıtı tek yerde (frontend config) durur ki bir admin aracı ya da başka istemci onu miras almasın.
    [HttpGet("countries")]
    [ProducesResponseType(typeof(List<CountryDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<List<CountryDto>?>> GetCountries(CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new GetCountriesBffQuery(), ct));
}
