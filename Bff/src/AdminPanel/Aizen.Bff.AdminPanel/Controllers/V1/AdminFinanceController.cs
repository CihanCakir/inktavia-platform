using Aizen.Bff.AdminPanel.Application.AdminFinance.Query.GetCargoDryCommissionRuleUsageBff;
using Aizen.Bff.AdminPanel.Application.AdminFinance.Query.GetCargoDryRenewalReconciliationBff;
using Aizen.Bff.AdminPanel.Application.AdminFinance.Query.GetCargoDrySettlementReconciliationBff;
using Aizen.Bff.AdminPanel.Application.AdminFinance.Query.GetPaymentInvoiceStatementBff;
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
public sealed class AdminFinanceController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public AdminFinanceController(
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
}
