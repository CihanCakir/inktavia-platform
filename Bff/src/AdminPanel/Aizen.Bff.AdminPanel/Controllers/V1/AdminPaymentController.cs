using Aizen.Core.Infrastructure.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.AdminPanel.Controllers.V1;

[ApiController]
[Route("api/v1/admin-panel")]
[Tags("Admin Panel - Payments")]
[Authorize]
public sealed class AdminPaymentController : AizenWebApiController
{
    public AdminPaymentController(IHttpContextAccessor httpContextAccessor)
        : base(httpContextAccessor) { }

    [HttpGet("payments/transactions")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public Task<AizenApiResponse<object>> GetTransactions(
        [FromQuery] string? status = null,
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        return Task.FromResult(SetResponse<object>(new { items = Array.Empty<object>(), totalCount = 0, moduleStatus = "not_yet_available" }));
    }

    [HttpGet("payments/transactions/kpi")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public Task<AizenApiResponse<object>> GetTransactionKpis(CancellationToken ct = default)
    {
        return Task.FromResult(SetResponse<object>(new { totalRevenue = 0, totalTransactions = 0, moduleStatus = "not_yet_available" }));
    }

    [HttpGet("payments/commissions")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public Task<AizenApiResponse<object>> GetCommissions(
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        return Task.FromResult(SetResponse<object>(new { items = Array.Empty<object>(), totalCount = 0, moduleStatus = "not_yet_available" }));
    }
}
