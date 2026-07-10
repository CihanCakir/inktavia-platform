using Aizen.Bff.MarineProvider.Application.Common.Authorization;
using Aizen.Bff.MarineProvider.Application.Contracts.Jobs;
using Aizen.Bff.MarineProvider.Application.Jobs.GetProviderJobs;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.MarineProvider.Controllers.V1;

/// <summary>
/// Provider workspace: assigned jobs. Requires an Approved + Active provider profile (runtime Identity status).
/// Provider identity is resolved server-side and asserted to the ServiceRequest module; the client cannot pass a
/// provider id.
/// </summary>
[ApiController]
[Route("api/v1/provider/jobs")]
[Tags("Provider - Jobs")]
[Authorize(Policy = ProviderAuthorizationPolicies.ProviderActive)]
public sealed class ProviderJobsController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public ProviderJobsController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    /// <summary>List the caller provider's assigned jobs (paged).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(GetProviderJobsResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetProviderJobsResponse>> GetJobs(
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new GetProviderJobsQuery(pageIndex, pageSize), ct);
        return SetResponse(result);
    }
}
