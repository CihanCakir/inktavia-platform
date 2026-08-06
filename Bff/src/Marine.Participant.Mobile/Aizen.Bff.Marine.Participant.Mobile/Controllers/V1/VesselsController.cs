using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Authorization;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Vessel;
using Aizen.Bff.Marine.Participant.Mobile.Application.Vessel;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.Marine.Participant.Mobile.Controllers.V1;

/// <summary>Participant-scoped vessel reads (M4a). List + detail; detail is gated to the caller's own vessels.</summary>
[ApiController]
[Route("api/v1/mobile/vessels")]
[Tags("Mobile - Vessels")]
[Authorize(Policy = ParticipantAuthorizationPolicies.ParticipantAuthenticated)]
public sealed class VesselsController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public VesselsController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    /// <summary>The caller's vessels (empty when none).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<MobileVesselListItemDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<List<MobileVesselListItemDto>>> GetVessels(CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new GetMobileVesselsQuery(), ct);
        return SetResponse(result);
    }

    /// <summary>Full detail for one of the caller's vessels; a foreign/unknown id yields a clean not-found business error.</summary>
    [HttpGet("{vesselId:long}")]
    [ProducesResponseType(typeof(MobileVesselDetailDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MobileVesselDetailDto>> GetVessel([FromRoute] long vesselId, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new GetMobileVesselDetailQuery(vesselId), ct);
        return SetResponse(result);
    }

    /// <summary>Create a vessel (core + optional spec + optional engine, M4b). The caller becomes its PrimaryOwner;
    /// returns the assembled detail. Spec/engine are best-effort — the returned detail reflects what persisted.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(MobileVesselDetailDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MobileVesselDetailDto>> CreateVessel(
        [FromBody] CreateMobileVesselRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new CreateMobileVesselCommand(request), ct);
        return SetResponse(result);
    }

    /// <summary>Update one of the caller's vessels (core + optional spec + optional engine, M4c). Patch semantics —
    /// omitted fields keep their current value. A foreign/unknown id yields a clean not-found; returns the updated
    /// detail so detail/list/Home reflect the change immediately.</summary>
    [HttpPut("{vesselId:long}")]
    [ProducesResponseType(typeof(MobileVesselDetailDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MobileVesselDetailDto>> UpdateVessel(
        [FromRoute] long vesselId, [FromBody] UpdateMobileVesselRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new UpdateMobileVesselCommand(vesselId, request), ct);
        return SetResponse(result);
    }
}
