using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Model.Result;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetFinanceInvoiceStatementReport;

[DocumentationInfo("Get finance invoice statement report query handler",
    "Returns a paged finance invoice statement report for admin finance auditing. " +
    "Loads invoices from the Payment module and computes mismatch flags server-side " +
    "from entity field inspection only. No cross-module calls are made. " +
    "Supports filtering by invoice type, status, source type, buyer, currency, date range, and search. " +
    "Summaries (currency-level totals) are computed from the full filtered set before pagination. " +
    "Phase 15 (July 2026).")]
public sealed class GetFinanceInvoiceStatementReportQueryHandler
    : AizenQueryHandler<GetFinanceInvoiceStatementReportQuery, GetFinanceInvoiceStatementReportResponse>
{
    private readonly IInvoiceRepository _invoices;

    // Stale-draft threshold: a Draft invoice older than this many days is flagged.
    private const int StaleDraftThresholdDays = 7;

    public GetFinanceInvoiceStatementReportQueryHandler(IInvoiceRepository invoices)
        => _invoices = invoices;

    public override async Task<GetFinanceInvoiceStatementReportResponse> Handle(
        GetFinanceInvoiceStatementReportQuery request, CancellationToken ct)
    {
        // Load all matching invoices — paginate after mismatch filtering.
        // GetPagedAsync supports: type, status, prefix (null here), buyerUserId, fromDate, toDate, search.
        // SourceType and Currency are applied in-memory below.
        var (items, _) = await _invoices.GetPagedAsync(
            type:        request.Type,
            status:      request.Status,
            prefix:      null,
            buyerUserId: request.BuyerUserId,
            fromDate:    request.FromDate,
            toDate:      request.ToDate,
            search:      request.Search,
            skip:        0,
            take:        int.MaxValue,
            ct);

        // In-memory post-filter for SourceType and Currency (not available as DB-level filters)
        var filtered = items.AsEnumerable();

        if (request.SourceType.HasValue)
            filtered = filtered.Where(i => i.SourceType == request.SourceType.Value);

        if (!string.IsNullOrWhiteSpace(request.Currency))
            filtered = filtered.Where(i =>
                string.Equals(i.Currency, request.Currency, StringComparison.OrdinalIgnoreCase));

        var allItems = filtered.ToList();

        // Snapshot UTC now once for consistent threshold comparisons
        var utcNow = DateTime.UtcNow;
        var staleDraftCutoff = utcNow.AddDays(-StaleDraftThresholdDays);

        // Compute mismatch flags server-side from entity field inspection only
        var rows = allItems
            .Select(inv =>
            {
                var flags    = new List<string>();
                var warnings = new List<string>();

                // ── Mismatch: issued/sent invoice is overdue and has remaining balance
                if (inv.RemainingAmount > 0m
                    && inv.Status is InvoiceStatus.Overdue
                        or InvoiceStatus.Sent
                    && inv.DueDateUtc.HasValue
                    && inv.DueDateUtc.Value < utcNow)
                    flags.Add("OverdueWithOutstandingBalance");

                // ── Mismatch: invoice marked Paid but RemainingAmount is not zero
                if (inv.Status == InvoiceStatus.Paid && inv.RemainingAmount != 0m)
                    flags.Add("PaidWithNonZeroRemaining");

                // ── Mismatch: Draft invoice that has been sitting for more than threshold days
                if (inv.Status == InvoiceStatus.Draft
                    && inv.CreateDate.HasValue
                    && inv.CreateDate.Value < staleDraftCutoff)
                    flags.Add("StaleUnissuedDraft");

                // ── Mismatch: Cancelled invoice still shows a PaidAmount
                if (inv.Status == InvoiceStatus.Cancelled && inv.PaidAmount > 0m)
                    flags.Add("CancelledWithPaidAmount");

                // ── Mismatch: buyer name is missing
                if (string.IsNullOrWhiteSpace(inv.BuyerName))
                    flags.Add("MissingBuyerName");

                // ── Mismatch: currency code is missing
                if (string.IsNullOrWhiteSpace(inv.Currency))
                    flags.Add("MissingCurrency");

                // ── Warning: issued invoice has no due date
                if (inv.IssueDateUtc.HasValue && !inv.DueDateUtc.HasValue)
                    warnings.Add("MissingDueDate");

                // ── Warning: issued or later invoice has no PDF reference
                if (inv.IssueDateUtc.HasValue && string.IsNullOrWhiteSpace(inv.PdfFileRef))
                    warnings.Add("NoPdfRef");

                return new FinanceInvoiceStatementRowDto
                {
                    InvoiceId               = inv.Id,
                    InvoiceNumber           = inv.InvoiceNumber,
                    InvoiceType             = inv.InvoiceType.ToString(),
                    Status                  = inv.Status.ToString(),
                    SourceType              = inv.SourceType.ToString(),
                    SourceId                = inv.SourceId,
                    BuyerUserId             = inv.BuyerUserId,
                    BuyerName               = inv.BuyerName ?? string.Empty,
                    SellerUserId            = inv.SellerUserId,
                    SellerName              = inv.SellerName ?? string.Empty,
                    PaymentTransactionId    = inv.PaymentTransactionId,
                    ProviderPayoutId        = inv.ProviderPayoutId,
                    OriginalInvoiceId       = inv.OriginalInvoiceId,
                    Currency                = inv.Currency ?? string.Empty,
                    SubTotalAmount          = inv.SubTotalAmount,
                    DiscountAmount          = inv.DiscountAmount,
                    TaxableAmount           = inv.TaxableAmount,
                    TaxAmount               = inv.TaxAmount,
                    TotalAmount             = inv.TotalAmount,
                    PaidAmount              = inv.PaidAmount,
                    RemainingAmount         = inv.RemainingAmount,
                    IssueDateUtc            = inv.IssueDateUtc,
                    DueDateUtc              = inv.DueDateUtc,
                    PaidAtUtc               = inv.PaidAtUtc,
                    CancelledAtUtc          = inv.CancelledAtUtc,
                    CreateDate              = inv.CreateDate,
                    ExternalInvoiceId       = inv.ExternalInvoiceId,
                    ExternalInvoiceProvider = inv.ExternalInvoiceProvider,
                    HasPdf                  = !string.IsNullOrWhiteSpace(inv.PdfFileRef),
                    MismatchFlags           = flags,
                    Warnings                = warnings,
                };
            })
            .ToList();

        // Compute currency-level summaries from the full filtered set (before mismatch filter)
        var summaries = rows
            .GroupBy(r => r.Currency)
            .Select(g => new FinanceInvoiceStatementCurrencySummaryDto
            {
                Currency              = g.Key,
                TotalGrossAmount      = g.Sum(r => r.TotalAmount),
                TotalPaidAmount       = g.Sum(r => r.PaidAmount),
                TotalRemainingAmount  = g.Sum(r => r.RemainingAmount),
                TotalTaxAmount        = g.Sum(r => r.TaxAmount),
                TotalDiscountAmount   = g.Sum(r => r.DiscountAmount),
                InvoiceCount          = g.Count(),
            })
            .OrderBy(s => s.Currency)
            .ToList();

        // Apply HasMismatches filter in-memory after computation
        if (request.HasMismatches == true)
            rows = rows.Where(r => r.MismatchFlags.Count > 0).ToList();
        else if (request.HasMismatches == false)
            rows = rows.Where(r => r.MismatchFlags.Count == 0).ToList();

        var mismatchCount  = rows.Count(r => r.MismatchFlags.Count > 0);
        var effectiveTotal = rows.Count;
        var skip           = (request.Page - 1) * request.PageSize;

        var pagedItems = rows
            .OrderByDescending(r => r.IssueDateUtc ?? r.CreateDate)
            .Skip(skip)
            .Take(request.PageSize)
            .ToList();

        return new GetFinanceInvoiceStatementReportResponse
        {
            Report = new FinanceInvoiceStatementReportDto
            {
                Items         = pagedItems,
                Summaries     = summaries,
                Total         = effectiveTotal,
                Page          = request.Page,
                PageSize      = request.PageSize,
                MismatchCount = mismatchCount,
            }
        };
    }
}
