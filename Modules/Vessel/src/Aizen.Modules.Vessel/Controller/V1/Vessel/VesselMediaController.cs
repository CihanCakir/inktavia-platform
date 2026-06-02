using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Vessel.Abstraction.Dto.Media;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Abstraction.Request.Media;
using Aizen.Modules.Vessel.Application.Command.Media;
using Aizen.Modules.Vessel.Application.Query.Media;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniUow.Paging;

namespace Aizen.Modules.Vessel.Controller.V1.Vessel;

[ApiController]
[Route("api/v1/vessels/{vesselId:long}/media")]
[Tags("Vessel - Media")]
[Authorize]
[DocumentationInfo("Vessel media endpoints", "Manage vessel media files.")]
public sealed class VesselMediaController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public VesselMediaController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IPaginate<VesselMediaDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<IPaginate<VesselMediaDto>?>> GetAll(
        [FromRoute] long vesselId,
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<IPaginate<VesselMediaDto>>(new GetVesselMediaQuery(vesselId, pageIndex, pageSize), ct);
        return SetResponse(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(VesselMediaDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<VesselMediaDto?>> Add([FromRoute] long vesselId, [FromBody] AddVesselMediaRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<VesselMediaDto>(new AddVesselMediaCommand(vesselId, req), ct);
        return SetResponse(result);
    }

    [HttpPut("{mediaId:long}")]
    [ProducesResponseType(typeof(VesselMediaDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<VesselMediaDto?>> Update([FromRoute] long vesselId, [FromRoute] long mediaId, [FromBody] UpdateVesselMediaRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<VesselMediaDto>(new UpdateVesselMediaCommand(vesselId, mediaId, req), ct);
        return SetResponse(result);
    }

    [HttpDelete("{mediaId:long}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<object>> Remove([FromRoute] long vesselId, [FromRoute] long mediaId, CancellationToken ct = default)
    {
        await _cqrs.ProcessAsync<bool>(new RemoveVesselMediaCommand(vesselId, mediaId), ct);
        return SetResponse<object>(new { success = true });
    }

    [HttpPatch("{mediaId:long}/set-cover")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<object>> SetCover([FromRoute] long vesselId, [FromRoute] long mediaId, CancellationToken ct = default)
    {
        await _cqrs.ProcessAsync<bool>(new SetCoverVesselMediaCommand(vesselId, mediaId), ct);
        return SetResponse<object>(new { success = true });
    }

    [HttpPatch("{mediaId:long}/sort-order")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<object>> ChangeSortOrder([FromRoute] long vesselId, [FromRoute] long mediaId, [FromBody] int sortOrder, CancellationToken ct = default)
    {
        await _cqrs.ProcessAsync<bool>(new ChangeVesselMediaSortOrderCommand(vesselId, mediaId, sortOrder), ct);
        return SetResponse<object>(new { success = true });
    }
}
