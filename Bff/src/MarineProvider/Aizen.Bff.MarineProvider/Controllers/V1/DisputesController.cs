using Aizen.Bff.MarineProvider.Application.Common.Authorization;
using Aizen.Bff.MarineProvider.Application.ServiceRequests;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Provider;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.MarineProvider.Controllers.V1;

/// <summary>
/// Provider workspace: the provider's own disputes (on the service requests they won) + the open/actionable count
/// the dashboard attention row reads. Requires an Approved + Active provider profile. Provider identity is resolved
/// server-side and asserted to the ServiceRequest module; the client cannot pass a provider id. Cost-free.
/// </summary>
[ApiController]
[Route("api/v1/provider/disputes")]
[Tags("Provider - Disputes")]
[Authorize(Policy = ProviderAuthorizationPolicies.ProviderActive)]
public sealed class DisputesController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public DisputesController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor) => _cqrs = cqrs;

    /// <summary>List the caller provider's disputes (paged, optionally by status) + a global open/actionable count.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(GetProviderDisputesResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetProviderDisputesResponse>> GetDisputes(
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        [FromQuery] ServiceRequestDisputeStatus? status = null,
        CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(
            new GetProviderDisputesBffQuery { PageIndex = pageIndex, PageSize = pageSize, Status = status }, ct));
}
