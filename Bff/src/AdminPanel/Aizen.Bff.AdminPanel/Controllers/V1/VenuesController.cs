using Aizen.Bff.AdminPanel.Application.Identity.Query;
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
[Authorize(Policy = "AdminPanelAccess")]
public sealed class VenuesController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public VenuesController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor) => _cqrs = cqrs;

    [HttpGet("identity/venues/profiles")]
    [ProducesResponseType(typeof(PagedVenueProfileResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<PagedVenueProfileResult>> SearchProfiles(
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new SearchVenueProfilesBffQuery(pageIndex, pageSize), ct);
        return SetResponse(result);
    }

    [HttpGet("identity/venues/profiles/{profileId:guid}")]
    [ProducesResponseType(typeof(VenueProfileResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<VenueProfileResult>> GetProfileById(
        Guid profileId, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new GetVenueProfileByIdBffQuery(profileId), ct);
        return SetResponse(result);
    }
}
