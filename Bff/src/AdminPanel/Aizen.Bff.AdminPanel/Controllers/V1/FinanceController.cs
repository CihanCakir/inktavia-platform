using Aizen.Bff.AdminPanel.Application.Finance.Query.ExportCargoDryCommissionRuleUsageBff;
using Aizen.Bff.AdminPanel.Application.Finance.Query.ExportCargoDryRenewalReconciliationBff;
using Aizen.Bff.AdminPanel.Application.Finance.Query.ExportCargoDrySettlementReconciliationBff;
using Aizen.Bff.AdminPanel.Application.Finance.Query.ExportPaymentInvoiceStatementBff;
using Aizen.Bff.AdminPanel.Application.Finance.Query.GetCargoDryCommissionRuleUsageBff;
using Aizen.Bff.AdminPanel.Application.Finance.Query.GetCargoDryRenewalReconciliationBff;
using Aizen.Bff.AdminPanel.Application.Finance.Query.GetCargoDrySettlementReconciliationBff;
using Aizen.Bff.AdminPanel.Application.Finance.Query.GetPaymentInvoiceStatementBff;
using Aizen.Bff.AdminPanel.Application.Finance.Query.GetFinancialSummaryReportBff;
using Aizen.Bff.AdminPanel.Application.Finance.Query.GetLedgerEntriesBff;
using Aizen.Bff.AdminPanel.Application.Finance.Dto;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Model.Result;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.AdminPanel.Controllers.V1;

/// <summary>
/// Admin Panel BFF endpoints for Phase 15 Finance Reconciliation and Reporting.
/// All endpoints are read-only. No mutations, payment execution, or financial calculations are performed here.
/// All totals and mismatch flags come from backend/BFF DTOs.
/// </summary>
[ApiController]
[Route("api/v1/admin-panel/finance")]
[Tags("Admin Panel - Finance")]
[Authorize(Policy = "AdminPanelAccess")]
public sealed class FinanceController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public FinanceController(
        IHttpContextAccessor httpContextAccessor,
        IAizenCQRSProcessor  cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    // ── CargoDry Settlement Reconciliation ───────────────────────────────────

    /// <summary>
    /// GET /api/v1/admin-panel/finance/cargodry/reconciliation/settlements
    /// Paged CargoDry settlement reconciliation report with server-side mismatch detection.
    /// </summary>
    [HttpGet("cargodry/reconciliation/settlements")]
    public async Task<AizenApiResponse<CargoDrySettlementReconciliationReportDto>> GetCargoDrySettlementReconciliation(
        [FromQuery] long?     providerProfileId = null,
        [FromQuery] string?   productCode       = null,
        [FromQuery] int?      status            = null,
        [FromQuery] bool?     hasMismatches     = null,
        [FromQuery] DateTime? dateFrom          = null,
        [FromQuery] DateTime? dateTo            = null,
        [FromQuery] int       page              = 1,
        [FromQuery] int       pageSize          = 50,
        CancellationToken     ct                = default)
    {
        var result = await _cqrs.ProcessAsync(new GetCargoDrySettlementReconciliationBffQuery
        {
            ProviderProfileId = providerProfileId,
            ProductCode       = productCode,
            Status            = status,
            HasMismatches     = hasMismatches,
            DateFrom          = dateFrom,
            DateTo            = dateTo,
            Page              = page,
            PageSize          = pageSize,
        }, ct);

        return SetResponse(result?.Report);
    }

    // ── CargoDry Renewal Reconciliation ──────────────────────────────────────

    /// <summary>
    /// GET /api/v1/admin-panel/finance/cargodry/reconciliation/renewals
    /// Paged CargoDry renewal reconciliation report with server-side mismatch detection.
    /// </summary>
    [HttpGet("cargodry/reconciliation/renewals")]
    public async Task<AizenApiResponse<CargoDryRenewalReconciliationReportDto>> GetCargoDryRenewalReconciliation(
        [FromQuery] string?          productCode        = null,
        [FromQuery] long?            ownerUserId        = null,
        [FromQuery] long?            vesselId           = null,
        [FromQuery] int?             status             = null,
        [FromQuery] int?             notificationStatus = null,
        [FromQuery] bool?            hasMismatches      = null,
        [FromQuery] DateTimeOffset?  dateFrom           = null,
        [FromQuery] DateTimeOffset?  dateTo             = null,
        [FromQuery] int              page               = 1,
        [FromQuery] int              pageSize           = 50,
        CancellationToken            ct                 = default)
    {
        var result = await _cqrs.ProcessAsync(new GetCargoDryRenewalReconciliationBffQuery
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

        return SetResponse(result?.Report);
    }

    // ── CargoDry Commission Rule Usage ───────────────────────────────────────

    /// <summary>
    /// GET /api/v1/admin-panel/finance/cargodry/reports/commission-rule-usage
    /// Grouped commission rule usage statistics for CargoDry sales attributions.
    /// </summary>
    [HttpGet("cargodry/reports/commission-rule-usage")]
    public async Task<AizenApiResponse<CargoDryCommissionRuleUsageReportDto>> GetCargoDryCommissionRuleUsage(
        [FromQuery] DateTime? dateFrom          = null,
        [FromQuery] DateTime? dateTo            = null,
        [FromQuery] long?     ruleId            = null,
        [FromQuery] string?   productCode       = null,
        [FromQuery] string?   salesChannel      = null,
        [FromQuery] long?     providerProfileId = null,
        [FromQuery] int       page              = 1,
        [FromQuery] int       pageSize          = 50,
        CancellationToken     ct                = default)
    {
        var result = await _cqrs.ProcessAsync(new GetCargoDryCommissionRuleUsageBffQuery
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

        return SetResponse(result?.Report);
    }

    // ── Payment Invoice Statement ─────────────────────────────────────────────

    /// <summary>
    /// GET /api/v1/admin-panel/finance/payment/reports/invoice-statement
    /// Paged Payment module invoice statement report with server-side mismatch detection.
    /// Currency-level summaries are included. No financial calculations in BFF or frontend.
    /// </summary>
    [HttpGet("payment/reports/invoice-statement")]
    public async Task<AizenApiResponse<FinanceInvoiceStatementReportDto>> GetPaymentInvoiceStatement(
        [FromQuery] InvoiceType?       type          = null,
        [FromQuery] InvoiceStatus?     status        = null,
        [FromQuery] InvoiceSourceType? sourceType    = null,
        [FromQuery] long?              buyerUserId   = null,
        [FromQuery] string?            currency      = null,
        [FromQuery] DateTime?          fromDate      = null,
        [FromQuery] DateTime?          toDate        = null,
        [FromQuery] string?            search        = null,
        [FromQuery] bool?              hasMismatches = null,
        [FromQuery] int                page          = 1,
        [FromQuery] int                pageSize      = 50,
        CancellationToken              ct            = default)
    {
        var result = await _cqrs.ProcessAsync(new GetPaymentInvoiceStatementBffQuery
        {
            Type          = type,
            Status        = status,
            SourceType    = sourceType,
            BuyerUserId   = buyerUserId,
            Currency      = currency,
            FromDate      = fromDate,
            ToDate        = toDate,
            Search        = search,
            HasMismatches = hasMismatches,
            Page          = page,
            PageSize      = pageSize,
        }, ct);

        return SetResponse(result?.Report);
    }

    // ── CSV Export Endpoints (Phase 16G) ─────────────────────────────────────

    /// <summary>
    /// GET /api/v1/admin-panel/finance/cargodry/reconciliation/settlements/export
    /// Downloads the full CargoDry settlement reconciliation report as CSV.
    /// </summary>
    [HttpGet("cargodry/reconciliation/settlements/export")]
    public async Task<IActionResult> ExportCargoDrySettlementReconciliation(
        [FromQuery] long?     providerProfileId = null,
        [FromQuery] string?   productCode       = null,
        [FromQuery] int?      status            = null,
        [FromQuery] bool?     hasMismatches     = null,
        [FromQuery] DateTime? dateFrom          = null,
        [FromQuery] DateTime? dateTo            = null,
        CancellationToken     ct                = default)
    {
        var result = await _cqrs.ProcessAsync(new ExportCargoDrySettlementReconciliationBffQuery
        {
            ProviderProfileId = providerProfileId,
            ProductCode       = productCode,
            Status            = status,
            HasMismatches     = hasMismatches,
            DateFrom          = dateFrom,
            DateTo            = dateTo,
        }, ct);

        return File(result!.Bytes, result.ContentType, result.FileName);
    }

    /// <summary>
    /// GET /api/v1/admin-panel/finance/cargodry/reconciliation/renewals/export
    /// Downloads the full CargoDry renewal reconciliation report as CSV.
    /// </summary>
    [HttpGet("cargodry/reconciliation/renewals/export")]
    public async Task<IActionResult> ExportCargoDryRenewalReconciliation(
        [FromQuery] string?          productCode        = null,
        [FromQuery] long?            ownerUserId        = null,
        [FromQuery] long?            vesselId           = null,
        [FromQuery] int?             status             = null,
        [FromQuery] int?             notificationStatus = null,
        [FromQuery] bool?            hasMismatches      = null,
        [FromQuery] DateTimeOffset?  dateFrom           = null,
        [FromQuery] DateTimeOffset?  dateTo             = null,
        CancellationToken            ct                 = default)
    {
        var result = await _cqrs.ProcessAsync(new ExportCargoDryRenewalReconciliationBffQuery
        {
            ProductCode        = productCode,
            OwnerUserId        = ownerUserId,
            VesselId           = vesselId,
            Status             = status,
            NotificationStatus = notificationStatus,
            HasMismatches      = hasMismatches,
            DateFrom           = dateFrom,
            DateTo             = dateTo,
        }, ct);

        return File(result!.Bytes, result.ContentType, result.FileName);
    }

    /// <summary>
    /// GET /api/v1/admin-panel/finance/cargodry/reports/commission-rule-usage/export
    /// Downloads the full CargoDry commission rule usage report as CSV.
    /// </summary>
    [HttpGet("cargodry/reports/commission-rule-usage/export")]
    public async Task<IActionResult> ExportCargoDryCommissionRuleUsage(
        [FromQuery] DateTime? dateFrom          = null,
        [FromQuery] DateTime? dateTo            = null,
        [FromQuery] long?     ruleId            = null,
        [FromQuery] string?   productCode       = null,
        [FromQuery] string?   salesChannel      = null,
        [FromQuery] long?     providerProfileId = null,
        CancellationToken     ct                = default)
    {
        var result = await _cqrs.ProcessAsync(new ExportCargoDryCommissionRuleUsageBffQuery
        {
            DateFrom          = dateFrom,
            DateTo            = dateTo,
            RuleId            = ruleId,
            ProductCode       = productCode,
            SalesChannel      = salesChannel,
            ProviderProfileId = providerProfileId,
        }, ct);

        return File(result!.Bytes, result.ContentType, result.FileName);
    }

    /// <summary>
    /// GET /api/v1/admin-panel/finance/payment/reports/invoice-statement/export
    /// Downloads the full Payment invoice statement report as CSV.
    /// </summary>
    [HttpGet("payment/reports/invoice-statement/export")]
    public async Task<IActionResult> ExportPaymentInvoiceStatement(
        [FromQuery] InvoiceType?       type          = null,
        [FromQuery] InvoiceStatus?     status        = null,
        [FromQuery] InvoiceSourceType? sourceType    = null,
        [FromQuery] long?              buyerUserId   = null,
        [FromQuery] string?            currency      = null,
        [FromQuery] DateTime?          fromDate      = null,
        [FromQuery] DateTime?          toDate        = null,
        [FromQuery] string?            search        = null,
        [FromQuery] bool?              hasMismatches = null,
        CancellationToken              ct            = default)
    {
        var result = await _cqrs.ProcessAsync(new ExportPaymentInvoiceStatementBffQuery
        {
            Type          = type,
            Status        = status,
            SourceType    = sourceType,
            BuyerUserId   = buyerUserId,
            Currency      = currency,
            FromDate      = fromDate,
            ToDate        = toDate,
            Search        = search,
            HasMismatches = hasMismatches,
        }, ct);

        return File(result!.Bytes, result.ContentType, result.FileName);
    }

    // ── Financial reporting: §15 summary + ledger drill-down (BE-P12) ─────────

    /// <summary>
    /// GET /api/v1/admin-panel/finance/reports/financial-summary
    /// §15 period summary: revenue/expense totals + NetMarketplaceContribution + per-line breakdown. VAT liability +
    /// provider-funded discount are surfaced separately (never in the Inktavia P&amp;L, §19.17). Read-only.
    /// </summary>
    [HttpGet("reports/financial-summary")]
    public async Task<AizenApiResponse<FinancialSummaryReportBffDto>> GetFinancialSummaryReport(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] string   currency = "TRY",
        CancellationToken     ct       = default)
    {
        var result = await _cqrs.ProcessAsync(new GetFinancialSummaryReportBffQuery
        {
            From     = from,
            To       = to,
            Currency = currency,
        }, ct);

        return SetResponse(result);
    }

    /// <summary>
    /// GET /api/v1/admin-panel/finance/reports/ledger-entries
    /// Paged audit drill-down over the append-only financial ledger, filterable by account line / source / provider /
    /// period. Read-only.
    /// </summary>
    [HttpGet("reports/ledger-entries")]
    public async Task<AizenApiResponse<LedgerEntriesPageBffDto>> GetLedgerEntries(
        [FromQuery] LedgerAccountLine? accountLine       = null,
        [FromQuery] LedgerSourceType?  sourceType        = null,
        [FromQuery] long?              providerProfileId = null,
        [FromQuery] DateTime?          from              = null,
        [FromQuery] DateTime?          to                = null,
        [FromQuery] string?            currency          = null,
        [FromQuery] int                page              = 1,
        [FromQuery] int                pageSize          = 50,
        CancellationToken              ct                = default)
    {
        var result = await _cqrs.ProcessAsync(new GetLedgerEntriesBffQuery
        {
            AccountLine       = accountLine,
            SourceType        = sourceType,
            ProviderProfileId = providerProfileId,
            From              = from,
            To                = to,
            Currency          = currency,
            Page              = page,
            PageSize          = pageSize,
        }, ct);

        return SetResponse(result);
    }
}
