using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Marina;
using Aizen.Modules.ReferenceData.Application.Marina.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.ReferenceData.Controller.V1.ReferenceData;

// Salt-okunur marina sorguları. Tek çağıran BFF'lerdir ve servis token'ını iletir (CurrencyController ile aynı
// niyet): kimlik doğrulanmış erişim. Marina kataloğu hassas değildir; yine de sessiz 401'i önlemek için
// [Authorize] açıkça duruyor.
[ApiController]
[Authorize]
[Route("api/v1/reference-data/marinas")]
[Tags("Marina")]
[DocumentationInfo("Marina read endpoints", "Nearest-marina and marina-by-id queries available to all authenticated callers.")]
public sealed class MarinaController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public MarinaController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    [HttpGet("nearby")]
    [ProducesResponseType(typeof(IReadOnlyList<MarinaNearbyDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<IReadOnlyList<MarinaNearbyDto>>> GetNearby(
        [FromQuery] double? lat,
        [FromQuery] double? lng,
        [FromQuery] int limit = 10,
        CancellationToken ct = default)
    {
        if (lat is null || lng is null)
            throw new AizenBusinessException("Query parameters 'lat' and 'lng' are required.");
        if (lat is < -90 or > 90)
            throw new AizenBusinessException("'lat' must be between -90 and 90.");
        if (lng is < -180 or > 180)
            throw new AizenBusinessException("'lng' must be between -180 and 180.");

        var result = await _cqrs.ProcessAsync<IReadOnlyList<MarinaNearbyDto>>(
            new GetNearbyMarinasQuery(lat.Value, lng.Value, limit), ct);
        return SetResponse(result);
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(MarinaDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MarinaDto?>> GetById([FromRoute] long id, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<MarinaDto?>(new GetMarinaByIdQuery(id), ct);
        return SetResponse(result);
    }
}
