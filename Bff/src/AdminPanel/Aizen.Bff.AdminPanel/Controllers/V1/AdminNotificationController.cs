using Aizen.Core.Infrastructure.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.AdminPanel.Controllers.V1;

[ApiController]
[Route("api/v1/admin-panel")]
[Tags("Admin Panel - Notifications")]
[Authorize]
public sealed class AdminNotificationController : AizenWebApiController
{
    public AdminNotificationController(IHttpContextAccessor httpContextAccessor)
        : base(httpContextAccessor) { }

    [HttpGet("notification-templates")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public Task<AizenApiResponse<object>> GetTemplates(
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        return Task.FromResult(SetResponse<object>(new { items = Array.Empty<object>(), totalCount = 0, moduleStatus = "not_yet_available" }));
    }

    [HttpPost("notification-templates")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    public Task<AizenApiResponse<object>> CreateTemplate(
        [FromBody] object request,
        CancellationToken ct = default)
    {
        return Task.FromResult(SetResponse<object>(new { created = false, moduleStatus = "not_yet_available" }));
    }
}
