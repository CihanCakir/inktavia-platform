using Aizen.Core.Infrastructure.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.AdminPanel.Controllers.V1;

[ApiController]
[Route("api/v1/admin-panel")]
[Tags("Admin Panel - CargoDry")]
[Authorize]
public sealed class AdminCargoDryController : AizenWebApiController
{
    public AdminCargoDryController(IHttpContextAccessor httpContextAccessor)
        : base(httpContextAccessor) { }

    [HttpGet("cargodry/kits")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public Task<AizenApiResponse<object>> GetKits(
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        // CargoDry module is not yet connected to the BFF.
        // Returns empty placeholder until the module is fully wired.
        return Task.FromResult(SetResponse<object>(new { items = Array.Empty<object>(), totalCount = 0, moduleStatus = "not_yet_available" }));
    }

    [HttpPost("cargodry/kits/activate")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public Task<AizenApiResponse<object>> ActivateKit(
        [FromBody] object request,
        CancellationToken ct = default)
    {
        return Task.FromResult(SetResponse<object>(new { activated = false, moduleStatus = "not_yet_available" }));
    }
}
