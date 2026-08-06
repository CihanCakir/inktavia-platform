using Aizen.Bff.AdminPanel.Application.Dashboard.Dto;
using Aizen.Bff.AdminPanel.Application.Dashboard.Query;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.AdminPanel.Controllers.V1;

[ApiController]
[Route("api/v1/admin-panel/dashboard")]
[Tags("Admin Panel - Dashboard")]
[Authorize(Policy = "AdminPanelAccess")]
public sealed class DashboardController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public DashboardController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor) => _cqrs = cqrs;

    [HttpGet("overview")]
    [ProducesResponseType(typeof(AdminDashboardOverviewResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AdminDashboardOverviewResponse>> GetDashboardOverview(CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new GetDashboardOverviewBffQuery(), ct);
        return SetResponse(result);
    }
}
