using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Bff.AdminPanel.Application.Finance.Query.ExportPaymentInvoiceStatementBff;

/// <summary>
/// BFF query that downloads the full Payment invoice statement report as a CSV file.
/// Applies the same filters as the paged report but uses no pagination (all rows returned).
/// Does not include gateway secrets or payment tokens.
/// Phase 16G (July 2026).
/// </summary>
public sealed class ExportPaymentInvoiceStatementBffQuery
    : AizenQuery<ExportPaymentInvoiceStatementBffResponse>
{
    public InvoiceType?       Type          { get; init; }
    public InvoiceStatus?     Status        { get; init; }
    public InvoiceSourceType? SourceType    { get; init; }
    public long?              BuyerUserId   { get; init; }
    public string?            Currency      { get; init; }
    public DateTime?          FromDate      { get; init; }
    public DateTime?          ToDate        { get; init; }
    public string?            Search        { get; init; }
    public bool?              HasMismatches { get; init; }
}

public sealed class ExportPaymentInvoiceStatementBffResponse
{
    public byte[] Bytes       { get; init; } = [];
    public string ContentType { get; init; } = "text/csv";
    public string FileName    { get; init; } = "export.csv";
}
