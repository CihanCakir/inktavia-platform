using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Jobs;
using Aizen.Modules.ServiceRequest.Application.Query.Jobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.ServiceRequest.Controller.V1.Jobs;

/// <summary>
/// Provider-scoped read: the caller provider's own assignments (jobs). Provider identity is taken from the
/// trusted request context (BFF assertion), never from route/query. Service-token authorized (called by the
/// MarineProvider BFF).
/// </summary>
[ApiController]
[Route("api/v1/service-requests/provider")]
[Tags("ServiceRequest - Provider Jobs")]
[Authorize]
public sealed class ProviderJobsController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public ProviderJobsController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    [HttpGet("jobs")]
    [ProducesResponseType(typeof(GetProviderJobsResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetProviderJobsResponse?>> GetJobs(
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<GetProviderJobsResponse>(
            new GetProviderJobsQuery(pageIndex, pageSize), ct);

        return SetResponse(result);
    }
}
