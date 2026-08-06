using Aizen.Bff.AdminPanel.Application.Identity.Query;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Modules.Identity.Abstraction.Dto.Organizer;
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

    public OrganizersController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor) => _cqrs = cqrs;

    [HttpGet("identity/organizers/profiles")]
    [ProducesResponseType(typeof(PagedOrganizerProfileResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<PagedOrganizerProfileResult>> SearchProfiles(
        [FromQuery] int     pageIndex      = 0,
        [FromQuery] int     pageSize       = 20,
        [FromQuery] string? searchTerm     = null,
        [FromQuery] string? approvalStatus = null,
        [FromQuery] string? status         = null,
        [FromQuery] string? city           = null,
        [FromQuery] string? country        = null,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new SearchOrganizerProfilesBffQuery(pageIndex, pageSize, searchTerm, approvalStatus, status, city, country), ct);
        return SetResponse(result);
    }

    [HttpGet("identity/organizers/profiles/{profileId:guid}")]
    [ProducesResponseType(typeof(OrganizerProfileResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<OrganizerProfileResult>> GetProfileById(
        Guid profileId, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new GetOrganizerProfileByIdBffQuery(profileId), ct);
        return SetResponse(result);
    }

    [HttpGet("identity/organizers/profiles/{profileId:guid}/with-user")]
    [ProducesResponseType(typeof(OrganizerProfileWithUserResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<OrganizerProfileWithUserResult>> GetProfileWithUser(
        Guid profileId, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new GetOrganizerProfileWithUserBffQuery(profileId), ct);
        return SetResponse(result);
    }

    /// <summary>
    /// Phase 27 — Provider Directory detail endpoint.
    /// Uses long profileId matching Identity module's numeric PK.
    /// Route suffix /detail distinguishes from the Guid-based /with-user route above.
    /// </summary>
    [HttpGet("identity/organizers/profiles/{profileId:long}/detail")]
    [ProducesResponseType(typeof(OrganizerProfileWithUserDetailDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<OrganizerProfileWithUserDetailDto>> GetProviderDirectoryDetail(
        long profileId, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new GetProviderDirectoryDetailBffQuery(profileId), ct);
        return SetResponse(result);
    }
}
