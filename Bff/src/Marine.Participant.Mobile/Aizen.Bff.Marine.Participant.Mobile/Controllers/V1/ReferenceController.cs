using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Authorization;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Reference;
using Aizen.Bff.Marine.Participant.Mobile.Application.Reference;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.Marine.Participant.Mobile.Controllers.V1;

/// <summary>Read-only reference lookups for mobile form dropdowns. Any authenticated participant may read.</summary>
[ApiController]
[Route("api/v1/mobile/reference")]
[Tags("Mobile - Reference")]
[Authorize(Policy = ParticipantAuthorizationPolicies.ParticipantAuthenticated)]
public sealed class ReferenceController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public ReferenceController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    /// <summary>Country options (code, name, dial code). Literal route — precedes the {groupCode} template.</summary>
    [HttpGet("countries")]
    [ProducesResponseType(typeof(List<CountryItemDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<List<CountryItemDto>>> GetCountries(CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new GetCountriesQuery(), ct);
        return SetResponse(result);
    }

    /// <summary>Lookup options for one group (VESSEL_TYPE / FUEL_TYPE / ENGINE_TYPE / HULL_MATERIAL / SERVICE_PROVIDER_CATEGORY).</summary>
    [HttpGet("{groupCode}")]
    [ProducesResponseType(typeof(List<ReferenceItemDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<List<ReferenceItemDto>>> GetItems([FromRoute] string groupCode, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new GetReferenceItemsQuery(groupCode), ct);
        return SetResponse(result);
    }
}
