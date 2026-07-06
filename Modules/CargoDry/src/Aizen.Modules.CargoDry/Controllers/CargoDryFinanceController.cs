using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Application.Queries.GetCargoDryCommissionRuleUsageReport;
using Aizen.Modules.CargoDry.Application.Queries.GetCargoDryRenewalReconciliationReport;
using Aizen.Modules.CargoDry.Application.Queries.GetCargoDrySettlementReconciliationReport;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.CargoDry.Controllers;

/// <summary>
/// Read-only finance reconciliation and reporting endpoints for the CargoDry module.
/// All endpoints are scoped to Admin/SuperAdmin. No mutations are allowed here.
/// Phase 15 (July 2026).
/// </summary>
[ApiController]
[Authorize(Roles = "Admin,SuperAdmin")]
[Route("api/v1/cargodry/finance")]
public sealed class CargoDryFinanceController : ControllerBase
{
    private readonly ISender _sender;

    public CargoDryFinanceController(ISender sender) => _sender = sender;

    // ── Settlement / Payout Reconciliation ───────────────────────────────────

    /// <summary>
    /// GET /api/v1/cargodry/finance/reconciliation/settlements
    /// Paged settlement-payout reconciliation report.
    /// Mismatch flags are computed server-side; no cross-module calls are made.
    /// </summary>
    [HttpGet("reconciliation/settlements")]
    public async Task<IActionResult> GetSettlementReconciliation(
        [FromQuery] long?                                providerProfileId = null,
        [FromQuery] string?                              productCode       = null,
        [FromQuery] CargoDrySellThroughSettlementStatus? status            = null,
        [FromQuery] bool?                                hasMismatches     = null,
        [FromQuery] DateTime?                            dateFrom          = null,
        [FromQuery] DateTime?                            dateTo            = null,
        [FromQuery] int                                  page              = 1,
        [FromQuery] int                                  pageSize          = 50,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetCargoDrySettlementReconciliationReportQuery
        {
            providerProfileId = providerProfileId,
            ProductCode       = productCode,
            Status            = status,
            HasMismatches     = hasMismatches,
            DateFrom          = dateFrom,
            DateTo            = dateTo,
            Page              = page,
            PageSize          = pageSize,
        }, ct);

        return Ok(result.Report);
    }

    // ── Renewal Billing Reconciliation ───────────────────────────────────────

    /// <summary>
    /// GET /api/v1/cargodry/finance/reconciliation/renewals
    /// Paged renewal billing reconciliation report.
    /// Mismatch flags are computed server-side; no cross-module calls are made.
    /// </summary>
    [HttpGet("reconciliation/renewals")]
    public async Task<IActionResult> GetRenewalReconciliation(
        [FromQuery] string?                             productCode        = null,
        [FromQuery] long?                               ownerUserId        = null,
        [FromQuery] long?                               vesselId           = null,
        [FromQuery] CargoDryRenewalPreparationStatus?   status             = null,
        [FromQuery] CargoDryRenewalNotificationStatus?  notificationStatus = null,
        [FromQuery] bool?                               hasMismatches      = null,
        [FromQuery] DateTimeOffset?                     dateFrom           = null,
        [FromQuery] DateTimeOffset?                     dateTo             = null,
        [FromQuery] int                                 page               = 1,
        [FromQuery] int                                 pageSize           = 50,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetCargoDryRenewalReconciliationReportQuery
        {
            ProductCode        = productCode,
            OwnerUserId        = ownerUserId,
            VesselId           = vesselId,
            Status             = status,
            NotificationStatus = notificationStatus,
            HasMismatches      = hasMismatches,
            DateFrom           = dateFrom,
            DateTo             = dateTo,
            Page               = page,
            PageSize           = pageSize,
        }, ct);

        return Ok(result.Report);
    }

    // ── Commission Rule Usage Report ─────────────────────────────────────────

    /// <summary>
    /// GET /api/v1/cargodry/finance/reports/commission-rule-usage
    /// Commission rule usage aggregated from sales attribution records.
    /// Grouped by ResolvedRuleId / ProductCode / SalesChannel / ProviderProfileId.
    /// </summary>
    [HttpGet("reports/commission-rule-usage")]
    public async Task<IActionResult> GetCommissionRuleUsage(
        [FromQuery] DateTime? dateFrom          = null,
        [FromQuery] DateTime? dateTo            = null,
        [FromQuery] long?     ruleId            = null,
        [FromQuery] string?   productCode       = null,
        [FromQuery] string?   salesChannel      = null,
        [FromQuery] long?     providerProfileId = null,
        [FromQuery] int       page              = 1,
        [FromQuery] int       pageSize          = 50,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetCargoDryCommissionRuleUsageReportQuery
        {
            DateFrom          = dateFrom,
            DateTo            = dateTo,
            RuleId            = ruleId,
            ProductCode       = productCode,
            SalesChannel      = salesChannel,
            ProviderProfileId = providerProfileId,
            Page              = page,
            PageSize          = pageSize,
        }, ct);

        return Ok(result.Report);
    }
}
