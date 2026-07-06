using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Application.Queries.GetFinanceInvoiceStatementReport;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.Payment.Controllers;

/// <summary>
/// Read-only finance reporting endpoints for the Payment module.
/// All endpoints are scoped to Admin/SuperAdmin. No mutations are allowed here.
/// Phase 15 (July 2026).
/// </summary>
[ApiController]
[Authorize(Roles = "Admin,SuperAdmin")]
[Route("api/v1/payment/finance")]
public sealed class PaymentFinanceController : ControllerBase
{
    private readonly ISender _sender;

    public PaymentFinanceController(ISender sender) => _sender = sender;

    // ── Invoice Statement Report ─────────────────────────────────────────────

    /// <summary>
    /// GET /api/v1/payment/finance/reports/invoice-statement
    /// Paged finance invoice statement report with mismatch detection.
    /// Mismatch flags are computed server-side from entity field inspection only.
    /// No cross-module calls are made.
    /// Summaries reflect currency-level totals across the full filtered set.
    /// </summary>
    [HttpGet("reports/invoice-statement")]
    public async Task<IActionResult> GetInvoiceStatementReport(
        [FromQuery] InvoiceType?       type        = null,
        [FromQuery] InvoiceStatus?     status      = null,
        [FromQuery] InvoiceSourceType? sourceType  = null,
        [FromQuery] long?              buyerUserId = null,
        [FromQuery] string?            currency    = null,
        [FromQuery] DateTime?          fromDate    = null,
        [FromQuery] DateTime?          toDate      = null,
        [FromQuery] string?            search      = null,
        [FromQuery] bool?              hasMismatches = null,
        [FromQuery] int                page        = 1,
        [FromQuery] int                pageSize    = 50,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetFinanceInvoiceStatementReportQuery
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

        return Ok(result.Report);
    }
}
