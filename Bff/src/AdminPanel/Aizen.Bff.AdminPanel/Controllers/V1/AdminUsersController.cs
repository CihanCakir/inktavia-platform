using Aizen.Bff.AdminPanel.Application.AdminUsers.Dto;
using Aizen.Bff.AdminPanel.Application.AdminUsers.Query;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.AdminPanel.Controllers.V1;

[ApiController]
[Route("api/v1/admin-panel")]
[Tags("Admin Panel - Users")]
[Authorize(Policy = "AdminPanelAccess")]
public sealed class UsersController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public UsersController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrsProcessor)
        : base(httpContextAccessor)
    {
        _cqrs = cqrsProcessor;
    }

    // P0 — User list (literal routes must be declared before parameterised routes)

    [HttpGet("admin/users/kpi")]
    [ProducesResponseType(typeof(AdminUserKpiBffResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AdminUserKpiBffResponse>> GetUsersKpi(CancellationToken ct = default)
    {
        var userToken = HttpContext.Request.Headers["X-Aizen-User-Token"].FirstOrDefault() ?? string.Empty;
        var result = await _cqrs.ProcessAsync(new GetAdminUserKpiBffQuery(userToken), ct);
        return SetResponse(result);
    }

    [HttpGet("admin/users")]
    [ProducesResponseType(typeof(AdminUserListBffResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AdminUserListBffResponse>> GetUsers(
        [FromQuery] string? search = null,
        [FromQuery] string? role = null,
        [FromQuery] string? status = null,
        [FromQuery] string? identityType = null,
        [FromQuery] string? registeredFrom = null,
        [FromQuery] string? registeredTo = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var userToken = HttpContext.Request.Headers["X-Aizen-User-Token"].FirstOrDefault() ?? string.Empty;
        var result = await _cqrs.ProcessAsync(
            new GetAdminUserListBffQuery(userToken, search, role, status, identityType, registeredFrom, registeredTo, page, pageSize), ct);
        return SetResponse(result);
    }

    // P1 — User detail

    [HttpGet("admin/users/{profileId:long}/quick")]
    [ProducesResponseType(typeof(AdminUserQuickBffResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<AizenApiResponse<AdminUserQuickBffResponse>> GetUserQuick(
        long profileId, CancellationToken ct = default)
    {
        var userToken = HttpContext.Request.Headers["X-Aizen-User-Token"].FirstOrDefault() ?? string.Empty;
        var result = await _cqrs.ProcessAsync(new GetAdminUserQuickBffQuery(profileId, userToken), ct);
        return SetResponse(result);
    }

    [HttpGet("admin/users/{profileId:long}")]
    [ProducesResponseType(typeof(AdminUserDetailBffResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<AizenApiResponse<AdminUserDetailBffResponse>> GetUserDetail(
        long profileId, CancellationToken ct = default)
    {
        var userToken = HttpContext.Request.Headers["X-Aizen-User-Token"].FirstOrDefault() ?? string.Empty;
        var result = await _cqrs.ProcessAsync(new GetAdminUserDetailBffQuery(profileId, userToken), ct);
        return SetResponse(result);
    }

    // P2 — Detail page tabs

    [HttpGet("admin/users/{profileId:long}/vessels")]
    [ProducesResponseType(typeof(AdminUserVesselsBffResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AdminUserVesselsBffResponse>> GetUserVessels(
        long profileId, CancellationToken ct = default)
    {
        var userToken = HttpContext.Request.Headers["X-Aizen-User-Token"].FirstOrDefault() ?? string.Empty;
        var result = await _cqrs.ProcessAsync(new GetAdminUserVesselsBffQuery(profileId, userToken), ct);
        return SetResponse(result);
    }

    [HttpGet("admin/users/{profileId:long}/activity")]
    [ProducesResponseType(typeof(AdminUserActivityBffResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AdminUserActivityBffResponse>> GetUserActivity(
        long profileId, CancellationToken ct = default)
    {
        var userToken = HttpContext.Request.Headers["X-Aizen-User-Token"].FirstOrDefault() ?? string.Empty;
        var result = await _cqrs.ProcessAsync(new GetAdminUserActivityBffQuery(profileId, userToken), ct);
        return SetResponse(result);
    }
}
