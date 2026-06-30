using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Vessel.Abstraction.Request.Media;
using Aizen.Modules.Vessel.Abstraction.Response.Media;
using Aizen.Modules.Vessel.Application.Command.Media;
using Aizen.Modules.Vessel.Application.Query.Media;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    [ProducesResponseType(typeof(GetVesselMediaResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetVesselMediaResponse?>> GetAll(
        [FromRoute] long vesselId,
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool includeAccessUrls = false,
        [FromQuery] int accessUrlExpiresInMinutes = 15,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<GetVesselMediaResponse>(new GetVesselMediaQuery(vesselId, pageIndex, pageSize, includeAccessUrls, accessUrlExpiresInMinutes), ct);
        return SetResponse(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(AddVesselMediaResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AddVesselMediaResponse?>> Add([FromRoute] long vesselId, [FromBody] AddVesselMediaRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<AddVesselMediaResponse>(new AddVesselMediaCommand(vesselId, req), ct);
        return SetResponse(result);
    }

    [HttpPut("{mediaId:long}")]
    [ProducesResponseType(typeof(UpdateVesselMediaResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<UpdateVesselMediaResponse?>> Update([FromRoute] long vesselId, [FromRoute] long mediaId, [FromBody] UpdateVesselMediaRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<UpdateVesselMediaResponse>(new UpdateVesselMediaCommand(vesselId, mediaId, req), ct);
        return SetResponse(result);
    }

    [HttpDelete("{mediaId:long}")]
    [ProducesResponseType(typeof(RemoveVesselMediaResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<RemoveVesselMediaResponse?>> Remove([FromRoute] long vesselId, [FromRoute] long mediaId, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<RemoveVesselMediaResponse>(new RemoveVesselMediaCommand(vesselId, mediaId), ct);
        return SetResponse(result);
    }

    [HttpPatch("{mediaId:long}/set-cover")]
    [ProducesResponseType(typeof(SetCoverVesselMediaResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<SetCoverVesselMediaResponse?>> SetCover([FromRoute] long vesselId, [FromRoute] long mediaId, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<SetCoverVesselMediaResponse>(new SetCoverVesselMediaCommand(vesselId, mediaId), ct);
        return SetResponse(result);
    }

    [HttpPatch("{mediaId:long}/sort-order")]
    [ProducesResponseType(typeof(ChangeVesselMediaSortOrderResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ChangeVesselMediaSortOrderResponse?>> ChangeSortOrder([FromRoute] long vesselId, [FromRoute] long mediaId, [FromBody] int sortOrder, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<ChangeVesselMediaSortOrderResponse>(new ChangeVesselMediaSortOrderCommand(vesselId, mediaId, sortOrder), ct);
        return SetResponse(result);
    }
}
