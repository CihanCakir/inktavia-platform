using Aizen.Core.Infrastructure.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.AdminPanel.Controllers.V1;

/// <summary>
/// Returns 501 Not Implemented for inactive or future module endpoints.
/// These modules are not yet active in the current platform release:
///   Notification (template management), Payment, Reporting, Analytics.
/// Routes are registered to avoid 404 responses and return a clear not-implemented envelope.
/// </summary>
[ApiController]
[Route("api/v1/admin-panel")]
[Tags("Admin Panel - Inactive Modules")]
[AllowAnonymous]
public sealed class InactiveModulesController : AizenWebApiController
{
    public InactiveModulesController(IHttpContextAccessor httpContextAccessor)
        : base(httpContextAccessor) { }

    // ─── Payment ─────────────────────────────────────────────────────────────

    [HttpGet("payments/transactions")]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public IActionResult GetPaymentTransactions() => NotImplementedEnvelope("Payment");

    [HttpGet("payments/transactions/kpi")]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public IActionResult GetPaymentTransactionKpi() => NotImplementedEnvelope("Payment");

    [HttpGet("payments/commissions")]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public IActionResult GetPaymentCommissions() => NotImplementedEnvelope("Payment");

    // ─── Reporting ────────────────────────────────────────────────────────────

    [HttpGet("reports")]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public IActionResult GetReports() => NotImplementedEnvelope("Reporting");

    [HttpGet("reports/kpi")]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public IActionResult GetReportsKpi() => NotImplementedEnvelope("Reporting");

    [HttpGet("analytics/dashboard")]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public IActionResult GetAnalyticsDashboard() => NotImplementedEnvelope("Analytics");

    // NOT: "files" listesi artık FilesController tarafından karşılanıyor (FileStorage'a proxy).
    // Eski 501 stub'ı kaldırıldı.

    // ─── Helper ──────────────────────────────────────────────────────────────

    private ObjectResult NotImplementedEnvelope(string moduleName) =>
        StatusCode(StatusCodes.Status501NotImplemented, new
        {
            header = new { isSuccess = false, errorCode = 501, message = $"Module '{moduleName}' is not active in this release." },
            body = (object?)null
        });
}
