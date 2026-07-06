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

    // ── CSV Export Endpoints (Phase 16G) ─────────────────────────────────────

    /// <summary>
    /// GET /api/v1/cargodry/finance/reconciliation/settlements/export
    /// Downloads the full (unpaginated) settlement reconciliation report as CSV.
    /// Preserves all active filters; no pagination — uses int.MaxValue to fetch all rows.
    /// </summary>
    [HttpGet("reconciliation/settlements/export")]
    public async Task<IActionResult> ExportSettlementReconciliation(
        [FromQuery] long?                                providerProfileId = null,
        [FromQuery] string?                              productCode       = null,
        [FromQuery] CargoDrySellThroughSettlementStatus? status            = null,
        [FromQuery] bool?                                hasMismatches     = null,
        [FromQuery] DateTime?                            dateFrom          = null,
        [FromQuery] DateTime?                            dateTo            = null,
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
            Page              = 1,
            PageSize          = int.MaxValue,
        }, ct);

        var csv = BuildSettlementCsv(result.Report.Items);
        return File(
            System.Text.Encoding.UTF8.GetBytes(csv),
            "text/csv",
            $"cargodry-settlement-reconciliation-{DateTimeOffset.UtcNow:yyyyMMdd}.csv");
    }

    /// <summary>
    /// GET /api/v1/cargodry/finance/reconciliation/renewals/export
    /// Downloads the full (unpaginated) renewal reconciliation report as CSV.
    /// </summary>
    [HttpGet("reconciliation/renewals/export")]
    public async Task<IActionResult> ExportRenewalReconciliation(
        [FromQuery] string?                             productCode        = null,
        [FromQuery] long?                               ownerUserId        = null,
        [FromQuery] long?                               vesselId           = null,
        [FromQuery] CargoDryRenewalPreparationStatus?   status             = null,
        [FromQuery] CargoDryRenewalNotificationStatus?  notificationStatus = null,
        [FromQuery] bool?                               hasMismatches      = null,
        [FromQuery] DateTimeOffset?                     dateFrom           = null,
        [FromQuery] DateTimeOffset?                     dateTo             = null,
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
            Page               = 1,
            PageSize           = int.MaxValue,
        }, ct);

        var csv = BuildRenewalCsv(result.Report.Items);
        return File(
            System.Text.Encoding.UTF8.GetBytes(csv),
            "text/csv",
            $"cargodry-renewal-reconciliation-{DateTimeOffset.UtcNow:yyyyMMdd}.csv");
    }

    /// <summary>
    /// GET /api/v1/cargodry/finance/reports/commission-rule-usage/export
    /// Downloads the full (unpaginated) commission rule usage report as CSV.
    /// </summary>
    [HttpGet("reports/commission-rule-usage/export")]
    public async Task<IActionResult> ExportCommissionRuleUsage(
        [FromQuery] DateTime? dateFrom          = null,
        [FromQuery] DateTime? dateTo            = null,
        [FromQuery] long?     ruleId            = null,
        [FromQuery] string?   productCode       = null,
        [FromQuery] string?   salesChannel      = null,
        [FromQuery] long?     providerProfileId = null,
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
            Page              = 1,
            PageSize          = int.MaxValue,
        }, ct);

        var csv = BuildCommissionRuleUsageCsv(result.Report.Items);
        return File(
            System.Text.Encoding.UTF8.GetBytes(csv),
            "text/csv",
            $"cargodry-commission-rule-usage-{DateTimeOffset.UtcNow:yyyyMMdd}.csv");
    }

    // ── CSV Builders ─────────────────────────────────────────────────────────

    private static string BuildSettlementCsv(
        IEnumerable<Aizen.Modules.CargoDry.Abstraction.Dto.CargoDrySettlementReconciliationRowDto> rows)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("SettlementId,SettlementCode,ProviderProfileId,ProductCode,CurrencyCode," +
                      "SettlementStatus,AttributionCount,TotalSaleAmount,ProviderPayoutAmount," +
                      "PlatformShareAmount,PayoutRecordId,PayoutStatus,InvoiceId,InvoiceStatus," +
                      "PaymentPreparedAtUtc,InvoicePreparedAtUtc,PayoutCompletedAtUtc,CreatedAtUtc," +
                      "MismatchFlags,Warnings");
        foreach (var r in rows)
            sb.AppendLine(
                $"{r.SettlementId},{Esc(r.SettlementCode)},{r.ProviderProfileId},{Esc(r.ProductCode)}," +
                $"{Esc(r.CurrencyCode)},{Esc(r.SettlementStatus)},{r.AttributionCount}," +
                $"{r.TotalSaleAmount:F2},{r.ProviderPayoutAmount:F2},{r.PlatformShareAmount:F2}," +
                $"{r.PayoutRecordId},{Esc(r.PayoutStatus)},{r.InvoiceId},{Esc(r.InvoiceStatus)}," +
                $"{r.PaymentPreparedAtUtc:O},{r.InvoicePreparedAtUtc:O},{r.PayoutCompletedAtUtc:O},{r.CreatedAtUtc:O}," +
                $"{Esc(string.Join("|", r.MismatchFlags))},{Esc(string.Join("|", r.Warnings))}");
        return sb.ToString();
    }

    private static string BuildRenewalCsv(
        IEnumerable<Aizen.Modules.CargoDry.Abstraction.Dto.CargoDryRenewalReconciliationRowDto> rows)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("RenewalPreparationId,RenewalCode,KitId,KitCode,OwnerUserId,VesselId," +
                      "ProductCode,Status,RenewalPrice,CurrencyCode,InvoiceId,ManualPaymentReference," +
                      "NotificationStatus,CompletedAtUtc,NewExpiresAtUtc,PreparedAtUtc," +
                      "MismatchFlags,Warnings");
        foreach (var r in rows)
            sb.AppendLine(
                $"{r.RenewalPreparationId},{Esc(r.RenewalCode)},{r.KitId},{Esc(r.KitCode)}," +
                $"{r.OwnerUserId},{r.VesselId},{Esc(r.ProductCode)},{Esc(r.Status)}," +
                $"{r.RenewalPrice:F2},{Esc(r.CurrencyCode)},{r.InvoiceId}," +
                $"{Esc(r.ManualPaymentReference)},{Esc(r.NotificationStatus)}," +
                $"{r.CompletedAtUtc:O},{r.NewExpiresAtUtc:O},{r.PreparedAtUtc:O}," +
                $"{Esc(string.Join("|", r.MismatchFlags))},{Esc(string.Join("|", r.Warnings))}");
        return sb.ToString();
    }

    private static string BuildCommissionRuleUsageCsv(
        IEnumerable<Aizen.Modules.CargoDry.Abstraction.Dto.CargoDryCommissionRuleUsageRowDto> rows)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("RuleId,RuleName,ResolvedRuleSource,ProductCode,SalesChannel," +
                      "ProviderProfileId,UsageCount,TotalSaleAmount,TotalProviderShareAmount," +
                      "TotalPlatformShareAmount,FirstUsedAtUtc,LastUsedAtUtc");
        foreach (var r in rows)
            sb.AppendLine(
                $"{r.RuleId},{Esc(r.RuleName)},{Esc(r.ResolvedRuleSource)},{Esc(r.ProductCode)}," +
                $"{Esc(r.SalesChannel)},{r.ProviderProfileId},{r.UsageCount}," +
                $"{r.TotalSaleAmount:F2},{r.TotalProviderShareAmount:F2},{r.TotalPlatformShareAmount:F2}," +
                $"{r.FirstUsedAtUtc:O},{r.LastUsedAtUtc:O}");
        return sb.ToString();
    }

    /// <summary>Escapes a value for CSV by wrapping in quotes if it contains commas, quotes, or newlines.</summary>
    private static string Esc(string? v)
    {
        if (v is null) return string.Empty;
        if (v.Contains(',') || v.Contains('"') || v.Contains('\n'))
            return $"\"{v.Replace("\"", "\"\"")}\"";
        return v;
    }
}
