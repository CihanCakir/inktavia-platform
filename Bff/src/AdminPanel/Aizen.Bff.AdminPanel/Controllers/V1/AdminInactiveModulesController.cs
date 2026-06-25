using Aizen.Core.Infrastructure.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.AdminPanel.Controllers.V1;

/// <summary>
/// Returns 501 Not Implemented for inactive or future module endpoints.
/// These modules are not yet active in the current platform release:
///   CargoDry, Notification (template management), Payment, Reporting, Analytics.
/// Routes are registered to avoid 404 responses and return a clear not-implemented envelope.
/// </summary>
[ApiController]
[Route("api/v1/admin-panel")]
[Tags("Admin Panel - Inactive Modules")]
[AllowAnonymous]
public sealed class AdminInactiveModulesController : AizenWebApiController
{
    public AdminInactiveModulesController(IHttpContextAccessor httpContextAccessor)
        : base(httpContextAccessor) { }

    // ─── CargoDry ────────────────────────────────────────────────────────────

    [HttpGet("cargodry/kits")]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public IActionResult GetCargoDryKits() => NotImplementedEnvelope("CargoDry");

    [HttpPost("cargodry/kits/activate")]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public IActionResult ActivateCargoDryKit() => NotImplementedEnvelope("CargoDry");

    // ─── Notification templates ───────────────────────────────────────────────

    [HttpGet("notification-templates")]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public IActionResult GetNotificationTemplates() => NotImplementedEnvelope("Notification");

    [HttpPost("notification-templates")]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public IActionResult CreateNotificationTemplate() => NotImplementedEnvelope("Notification");

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

    // ─── Files list (not yet exposed as admin listing endpoint) ──────────────

    [HttpGet("files")]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public IActionResult GetFilesList() => NotImplementedEnvelope("FileStorage list");

    // ─── Helper ──────────────────────────────────────────────────────────────

    private ObjectResult NotImplementedEnvelope(string moduleName) =>
        StatusCode(StatusCodes.Status501NotImplemented, new
        {
            header = new { isSuccess = false, errorCode = 501, message = $"Module '{moduleName}' is not active in this release." },
            body = (object?)null
        });
}
