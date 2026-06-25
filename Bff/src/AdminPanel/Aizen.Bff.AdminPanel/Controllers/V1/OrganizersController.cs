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
[Tags("Admin Panel - Organizers")]
[Authorize(Policy = "AdminPanelAccess")]
public sealed class OrganizersController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public OrganizersController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrsProcessor)
        : base(httpContextAccessor)
    {
        _cqrs = cqrsProcessor;
    }

    [HttpGet("identity/organizers/profiles")]
    [ProducesResponseType(typeof(PagedOrganizerProfileResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<PagedOrganizerProfileResult>> SearchProfiles(
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var userToken = HttpContext.Request.Headers["X-Aizen-User-Token"].FirstOrDefault() ?? string.Empty;
        var result = await _cqrs.ProcessAsync(new SearchOrganizerProfilesQuery(userToken, pageIndex, pageSize), ct);
        return SetResponse(result);
    }

    [HttpGet("identity/organizers/profiles/{profileId:guid}")]
    [ProducesResponseType(typeof(OrganizerProfileResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<OrganizerProfileResult>> GetProfileById(
        Guid profileId, CancellationToken ct)
    {
        var userToken = HttpContext.Request.Headers["X-Aizen-User-Token"].FirstOrDefault() ?? string.Empty;
        var result = await _cqrs.ProcessAsync(new GetOrganizerProfileByIdQuery(profileId, userToken), ct);
        return SetResponse(result);
    }

    [HttpGet("identity/organizers/profiles/{profileId:guid}/with-user")]
    [ProducesResponseType(typeof(OrganizerProfileWithUserResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<OrganizerProfileWithUserResult>> GetProfileWithUser(
        Guid profileId, CancellationToken ct)
    {
        var userToken = HttpContext.Request.Headers["X-Aizen-User-Token"].FirstOrDefault() ?? string.Empty;
        var result = await _cqrs.ProcessAsync(new GetOrganizerProfileWithUserQuery(profileId, userToken), ct);
        return SetResponse(result);
    }
}
