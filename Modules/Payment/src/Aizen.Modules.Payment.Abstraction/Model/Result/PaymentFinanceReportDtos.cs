using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Abstraction.Model.Result;

/// <summary>
/// One row in the finance invoice statement report.
/// MismatchFlags are computed server-side from entity field inspection only.
/// No cross-module calls; all values come directly from InvoiceHeaderEntity fields.
/// Phase 15 (July 2026).
/// </summary>
public sealed class FinanceInvoiceStatementRowDto
{
    public long              InvoiceId               { get; init; }
    public string?           InvoiceNumber           { get; init; }
    public string            InvoiceType             { get; init; } = default!;
    public string            Status                  { get; init; } = default!;
    public string            SourceType              { get; init; } = default!;
    public long?             SourceId                { get; init; }

    // Parties
    public long?             BuyerUserId             { get; init; }
    public string            BuyerName               { get; init; } = default!;
    public long?             SellerUserId            { get; init; }
    public string            SellerName              { get; init; } = default!;

    // Linkage
    public long?             PaymentTransactionId    { get; init; }
    public long?             ProviderPayoutId        { get; init; }
    public long?             OriginalInvoiceId       { get; init; }

    // Amounts
    public string            Currency                { get; init; } = default!;
    public decimal           SubTotalAmount          { get; init; }
    public decimal           DiscountAmount          { get; init; }
    public decimal           TaxableAmount           { get; init; }
    public decimal           TaxAmount               { get; init; }
    public decimal           TotalAmount             { get; init; }
    public decimal           PaidAmount              { get; init; }
    public decimal           RemainingAmount         { get; init; }

    // Dates
    public DateTime?         IssueDateUtc            { get; init; }
    public DateTime?         DueDateUtc              { get; init; }
    public DateTime?         PaidAtUtc               { get; init; }
    public DateTime?         CancelledAtUtc          { get; init; }
    public DateTime?         CreateDate              { get; init; }

    // External refs
    public string?           ExternalInvoiceId       { get; init; }
    public string?           ExternalInvoiceProvider { get; init; }
    public bool              HasPdf                  { get; init; }

    // Reconciliation
    public List<string>      MismatchFlags           { get; init; } = new();
    public List<string>      Warnings                { get; init; } = new();
}

/// <summary>
/// Currency-level totals summary for the finance invoice statement.
/// Computed from the full filtered set before pagination.
/// Phase 15 (July 2026).
/// </summary>
public sealed class FinanceInvoiceStatementCurrencySummaryDto
{
    public string  Currency           { get; init; } = default!;
    public decimal TotalGrossAmount   { get; init; }
    public decimal TotalPaidAmount    { get; init; }
    public decimal TotalRemainingAmount { get; init; }
    public decimal TotalTaxAmount     { get; init; }
    public decimal TotalDiscountAmount { get; init; }
    public int     InvoiceCount       { get; init; }
}

/// <summary>
/// Paged finance invoice statement report.
/// Items are paginated; Summaries and MismatchCount reflect the full filtered set.
/// Phase 15 (July 2026).
/// </summary>
public sealed class FinanceInvoiceStatementReportDto
{
    public List<FinanceInvoiceStatementRowDto>          Items         { get; init; } = new();
    public List<FinanceInvoiceStatementCurrencySummaryDto> Summaries  { get; init; } = new();
    public int                                          Total         { get; init; }
    public int                                          Page          { get; init; }
    public int                                          PageSize      { get; init; }
    public int                                          MismatchCount { get; init; }
}
