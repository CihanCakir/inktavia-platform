using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Model.Result;

namespace Aizen.Modules.Payment.Application.Queries.GetFinanceInvoiceStatementReport;

/// <summary>
/// Returns a paged finance invoice statement report with mismatch detection.
/// Mismatch flags are computed server-side from InvoiceHeaderEntity field inspection only.
/// No cross-module calls. Phase 15 (July 2026).
/// </summary>
public sealed class GetFinanceInvoiceStatementReportQuery
    : AizenQuery<GetFinanceInvoiceStatementReportResponse>
{
    public InvoiceType?       Type        { get; init; }
    public InvoiceStatus?     Status      { get; init; }
    public InvoiceSourceType? SourceType  { get; init; }
    public long?              BuyerUserId { get; init; }
    public string?            Currency    { get; init; }
    public DateTime?          FromDate    { get; init; }
    public DateTime?          ToDate      { get; init; }
    public string?            Search      { get; init; }

    /// <summary>When true, only rows with at least one MismatchFlag are returned.</summary>
    public bool? HasMismatches { get; init; }

    public int Page     { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}

public sealed class GetFinanceInvoiceStatementReportResponse
{
    public FinanceInvoiceStatementReportDto Report { get; init; } = default!;
}
