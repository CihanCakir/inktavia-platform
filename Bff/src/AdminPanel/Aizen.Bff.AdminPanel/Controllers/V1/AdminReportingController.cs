using Aizen.Core.Infrastructure.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.AdminPanel.Controllers.V1;

[ApiController]
[Route("api/v1/admin-panel")]
[Tags("Admin Panel - Reporting")]
[Authorize]
public sealed class AdminReportingController : AizenWebApiController
{
    public AdminReportingController(IHttpContextAccessor httpContextAccessor)
        : base(httpContextAccessor) { }

    [HttpGet("reports")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public Task<AizenApiResponse<object>> GetReports(
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        return Task.FromResult(SetResponse<object>(new { items = Array.Empty<object>(), totalCount = 0, moduleStatus = "not_yet_available" }));
    }

    [HttpGet("reports/kpi")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public Task<AizenApiResponse<object>> GetKpis(CancellationToken ct = default)
    {
        return Task.FromResult(SetResponse<object>(new { moduleStatus = "not_yet_available" }));
    }

    [HttpGet("analytics/dashboard")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public Task<AizenApiResponse<object>> GetAnalyticsDashboard(CancellationToken ct = default)
    {
        return Task.FromResult(SetResponse<object>(new { moduleStatus = "not_yet_available" }));
    }
}
