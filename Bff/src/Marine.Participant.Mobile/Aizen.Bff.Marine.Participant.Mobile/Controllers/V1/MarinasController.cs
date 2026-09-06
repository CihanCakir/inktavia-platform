using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Authorization;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Marina;
using Aizen.Bff.Marine.Participant.Mobile.Application.Marina;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Core.Infrastructure.Exception;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.Marine.Participant.Mobile.Controllers.V1;

/// <summary>Participant-scoped marina lookup. Backs the "suggest the nearest marina" flow on the vessel-location screen.</summary>
[ApiController]
[Route("api/v1/mobile/marinas")]
[Tags("Mobile - Marinas")]
[Authorize(Policy = ParticipantAuthorizationPolicies.ParticipantAuthenticated)]
public sealed class MarinasController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public MarinasController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    /// <summary>Nearest marinas to a device position, ordered by ascending distance (default 10).</summary>
    [HttpGet("nearby")]
    [ProducesResponseType(typeof(List<MobileNearbyMarinaDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<List<MobileNearbyMarinaDto>>> GetNearby(
        [FromQuery] double? lat, [FromQuery] double? lng, [FromQuery] int limit = 10, CancellationToken ct = default)
    {
        if (lat is null || lng is null)
            throw new AizenBusinessException("Query parameters 'lat' and 'lng' are required.");

        var result = await _cqrs.ProcessAsync(new GetMobileNearbyMarinasQuery(lat.Value, lng.Value, limit), ct);
        return SetResponse(result);
    }
}
