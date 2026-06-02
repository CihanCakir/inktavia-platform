using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Abstraction.Response.Status;
using Aizen.Modules.Vessel.Application.Query.Status;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.Vessel.Controller.V1.Vessel;

[ApiController]
[Route("api/v1/vessels/{vesselId:long}/status-history")]
[Tags("Vessel - Status")]
[Authorize]
[DocumentationInfo("Vessel status history endpoints", "Query vessel status change history.")]
public sealed class VesselStatusController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public VesselStatusController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    [HttpGet]
    [ProducesResponseType(typeof(GetVesselStatusHistoryResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetVesselStatusHistoryResponse?>> GetHistory(
        [FromRoute] long vesselId,
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<GetVesselStatusHistoryResponse>(new GetVesselStatusHistoryQuery(vesselId, pageIndex, pageSize), ct);
        return SetResponse(result);
    }
}
