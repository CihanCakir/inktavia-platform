using Aizen.Bff.AdminPanel.Application.AdminIdentity.Query;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.AdminPanel.Controllers.V1;

[ApiController]
[Route("api/v1/admin-panel")]
[Tags("Admin Panel - Venues")]
[Authorize]
public sealed class VenuesController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public VenuesController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrsProcessor)
        : base(httpContextAccessor)
    {
        _cqrs = cqrsProcessor;
    }

    [HttpGet("identity/venues/profiles")]
    [ProducesResponseType(typeof(PagedVenueProfileResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<PagedVenueProfileResult>> SearchProfiles(
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var userToken = HttpContext.Request.Headers["X-Aizen-User-Token"].FirstOrDefault() ?? string.Empty;
        var result = await _cqrs.ProcessAsync(new SearchVenueProfilesQuery(userToken, pageIndex, pageSize), ct);
        return SetResponse(result);
    }

    [HttpGet("identity/venues/profiles/{profileId:guid}")]
    [ProducesResponseType(typeof(VenueProfileResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<VenueProfileResult>> GetProfileById(
        Guid profileId, CancellationToken ct)
    {
        var userToken = HttpContext.Request.Headers["X-Aizen-User-Token"].FirstOrDefault() ?? string.Empty;
        var result = await _cqrs.ProcessAsync(new GetVenueProfileByIdQuery(profileId, userToken), ct);
        return SetResponse(result);
    }
}
