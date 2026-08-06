using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Model.Result;

namespace Aizen.Bff.AdminPanel.Application.Finance.Query.GetPaymentInvoiceStatementBff;

/// <summary>
/// BFF query for the Payment module finance invoice statement report.
/// Proxies to GET /api/v1/payment/finance/reports/invoice-statement.
/// Mismatch detection is performed server-side in the Payment module handler.
/// Phase 15 (July 2026).
/// </summary>
public sealed class GetPaymentInvoiceStatementBffQuery
    : AizenQuery<GetPaymentInvoiceStatementBffResponse>
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
    public int                Page          { get; init; } = 1;
    public int                PageSize      { get; init; } = 50;
}

public sealed class GetPaymentInvoiceStatementBffResponse
{
    public FinanceInvoiceStatementReportDto Report { get; init; } = default!;
}
