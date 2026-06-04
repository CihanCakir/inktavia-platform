using Aizen.Bff.AdminPanel.Application.AdminDashboard.Dto;
using Aizen.Bff.AdminPanel.Application.AdminDashboard.Query;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.AdminPanel.Controllers.V1;

[ApiController]
[Route("api/v1/admin-panel")]
[Tags("Admin Panel - Dashboard")]
[Authorize(Roles = "Admin")]
public sealed class AdminDashboardController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public AdminDashboardController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrsProcessor)
        : base(httpContextAccessor)
    {
        _cqrs = cqrsProcessor;
    }

    [HttpGet("dashboard/overview")]
    [ProducesResponseType(typeof(AdminDashboardOverviewResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AdminDashboardOverviewResponse>> GetDashboardOverview(CancellationToken ct)
    {
        var auth = HttpContext.Request.Headers["Authorization"].FirstOrDefault() ?? string.Empty;
        var result = await _cqrs.ProcessAsync(new GetAdminDashboardOverviewQuery(auth), ct);
        return SetResponse(result);
    }
}
