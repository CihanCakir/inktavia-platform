using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Model.Result;
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

    // ── CSV Export (Phase 16G) ────────────────────────────────────────────────

    /// <summary>
    /// GET /api/v1/payment/finance/reports/invoice-statement/export
    /// Downloads the full (unpaginated) invoice statement as CSV.
    /// Preserves all active filters; no pagination — uses int.MaxValue.
    /// Does not include sensitive gateway secrets or payment tokens.
    /// </summary>
    [HttpGet("reports/invoice-statement/export")]
    public async Task<IActionResult> ExportInvoiceStatementReport(
        [FromQuery] InvoiceType?       type          = null,
        [FromQuery] InvoiceStatus?     status        = null,
        [FromQuery] InvoiceSourceType? sourceType    = null,
        [FromQuery] long?              buyerUserId   = null,
        [FromQuery] string?            currency      = null,
        [FromQuery] DateTime?          fromDate      = null,
        [FromQuery] DateTime?          toDate        = null,
        [FromQuery] string?            search        = null,
        [FromQuery] bool?              hasMismatches = null,
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
            Page          = 1,
            PageSize      = int.MaxValue,
        }, ct);

        var csv = BuildInvoiceCsv(result.Report.Items);
        return File(
            System.Text.Encoding.UTF8.GetBytes(csv),
            "text/csv",
            $"payment-invoice-statement-{DateTimeOffset.UtcNow:yyyyMMdd}.csv");
    }

    // ── CSV Builder ───────────────────────────────────────────────────────────

    private static string BuildInvoiceCsv(IEnumerable<FinanceInvoiceStatementRowDto> rows)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("InvoiceId,InvoiceNumber,InvoiceType,Status,SourceType,SourceId," +
                      "BuyerUserId,BuyerName,SellerUserId,SellerName," +
                      "PaymentTransactionId,ProviderPayoutId,OriginalInvoiceId," +
                      "Currency,SubTotalAmount,DiscountAmount,TaxableAmount,TaxAmount," +
                      "TotalAmount,PaidAmount,RemainingAmount," +
                      "IssueDateUtc,DueDateUtc,PaidAtUtc,CancelledAtUtc,CreateDate," +
                      "ExternalInvoiceId,ExternalInvoiceProvider,HasPdf," +
                      "MismatchFlags,Warnings");
        foreach (var r in rows)
            sb.AppendLine(
                $"{r.InvoiceId},{Esc(r.InvoiceNumber)},{Esc(r.InvoiceType)},{Esc(r.Status)}," +
                $"{Esc(r.SourceType)},{r.SourceId}," +
                $"{r.BuyerUserId},{Esc(r.BuyerName)},{r.SellerUserId},{Esc(r.SellerName)}," +
                $"{r.PaymentTransactionId},{r.ProviderPayoutId},{r.OriginalInvoiceId}," +
                $"{Esc(r.Currency)},{r.SubTotalAmount:F2},{r.DiscountAmount:F2},{r.TaxableAmount:F2}," +
                $"{r.TaxAmount:F2},{r.TotalAmount:F2},{r.PaidAmount:F2},{r.RemainingAmount:F2}," +
                $"{r.IssueDateUtc:O},{r.DueDateUtc:O},{r.PaidAtUtc:O},{r.CancelledAtUtc:O},{r.CreateDate:O}," +
                $"{Esc(r.ExternalInvoiceId)},{Esc(r.ExternalInvoiceProvider)},{r.HasPdf}," +
                $"{Esc(string.Join("|", r.MismatchFlags))},{Esc(string.Join("|", r.Warnings))}");
        return sb.ToString();
    }

    private static string Esc(string? v)
    {
        if (v is null) return string.Empty;
        if (v.Contains(',') || v.Contains('"') || v.Contains('\n'))
            return $"\"{v.Replace("\"", "\"\"")}\"";
        return v;
    }
}
