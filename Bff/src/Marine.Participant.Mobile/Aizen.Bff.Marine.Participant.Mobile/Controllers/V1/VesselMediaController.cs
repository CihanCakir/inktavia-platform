using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Authorization;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Vessel;
using Aizen.Bff.Marine.Participant.Mobile.Application.Vessel;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.Marine.Participant.Mobile.Controllers.V1;

/// <summary>Participant-scoped vessel photos (M4f). List (presigned read URLs) + attach (of a client-side presigned
/// upload) + delete + set-cover; all owner-gated to the caller's own vessels (foreign/unknown id → clean not-found).
/// The photo BYTES are uploaded client-side directly to storage (see /mobile/uploads) — never through this BFF.</summary>
[ApiController]
[Route("api/v1/mobile/vessels/{vesselId:long}/media")]
[Tags("Mobile - Vessel Media")]
[Authorize(Policy = ParticipantAuthorizationPolicies.ParticipantAuthenticated)]
public sealed class VesselMediaController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public VesselMediaController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    /// <summary>The caller's photos for this vessel (cover first; each with a fresh presigned read URL). Fresh
    /// immediately after attach/delete/set-cover.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<MobileVesselMediaDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<List<MobileVesselMediaDto>>> GetMedia(
        [FromRoute] long vesselId, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new GetMobileVesselMediaQuery(vesselId), ct);
        return SetResponse(result);
    }

    /// <summary>Attach a completed upload (fileId) as a vessel photo; returns the created media with a read URL.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(MobileVesselMediaDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MobileVesselMediaDto>> AttachMedia(
        [FromRoute] long vesselId, [FromBody] AttachMobileVesselMediaRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new AttachMobileVesselMediaCommand(vesselId, request), ct);
        return SetResponse(result);
    }

    /// <summary>Delete one of the caller's vessel photos; returns the deleted media id.</summary>
    [HttpDelete("{mediaId:long}")]
    [ProducesResponseType(typeof(MobileVesselMediaDeletedDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MobileVesselMediaDeletedDto>> DeleteMedia(
        [FromRoute] long vesselId, [FromRoute] long mediaId, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new DeleteMobileVesselMediaCommand(vesselId, mediaId), ct);
        return SetResponse(result);
    }

    /// <summary>Set a photo as the vessel cover; returns the re-read gallery (list/Home cover updates immediately).</summary>
    [HttpPost("{mediaId:long}/cover")]
    [ProducesResponseType(typeof(List<MobileVesselMediaDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<List<MobileVesselMediaDto>>> SetCover(
        [FromRoute] long vesselId, [FromRoute] long mediaId, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new SetCoverMobileVesselMediaCommand(vesselId, mediaId), ct);
        return SetResponse(result);
    }
}
