using Aizen.Bff.MarineProvider.Application.Common.Authorization;
using Aizen.Bff.MarineProvider.Application.ServiceRequests;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.MarineProvider.Controllers.V1;

[ApiController]
[Route("api/v1/provider/service-requests")]
[Tags("Provider - Service Requests")]
[Authorize(Policy = ProviderAuthorizationPolicies.ProviderActive)]
public sealed class ProviderServiceRequestsController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public ProviderServiceRequestsController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    [HttpGet("open")]
    public async Task<IActionResult> GetOpen(
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? serviceCategoryCode = null,
        [FromQuery] string? locationCityCode = null,
        [FromQuery] string? locationCountryCode = null,
        [FromQuery] string? searchTerm = null,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new GetOpenServiceRequestsBffQuery
        {
            PageIndex = pageIndex,
            PageSize = pageSize,
            ServiceCategoryCode = serviceCategoryCode,
            LocationCityCode = locationCityCode,
            LocationCountryCode = locationCountryCode,
            SearchTerm = searchTerm,
        }, ct);
        return Ok(SetResponse(result));
    }

    /// <summary>
    /// One request. The module rejects it when the calling provider has no relationship with it — a provider must
    /// not be able to read an arbitrary request by guessing an id.
    /// </summary>
    [HttpGet("{serviceRequestId:long}")]
    public async Task<IActionResult> GetDetail([FromRoute] long serviceRequestId, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new GetServiceRequestDetailBffQuery { ServiceRequestId = serviceRequestId }, ct);

        return Ok(SetResponse(result));
    }
}
