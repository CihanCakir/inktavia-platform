using Aizen.Bff.MarineProvider.Application.Common.Authorization;
using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Core.Infrastructure.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.MarineProvider.Controllers.V1;

[ApiController]
[Route("api/v1/provider/location")]
[Tags("Provider - Location")]
[Authorize(Policy = ProviderAuthorizationPolicies.ProviderAuthenticated)]
public sealed class ProviderLocationController : AizenWebApiController
{
    private readonly IProviderReferenceDataRemoteCall _referenceData;

    public ProviderLocationController(
        IHttpContextAccessor httpContextAccessor,
        IProviderReferenceDataRemoteCall referenceData)
        : base(httpContextAccessor)
    {
        _referenceData = referenceData;
    }

    [HttpGet("cities")]
    public async Task<IActionResult> GetCities(
        [FromQuery] string country = "TR",
        CancellationToken ct = default)
    {
        var result = await _referenceData.GetCitiesByCountry(country);
        return Ok(SetResponse(result.Body));
    }
}
