using Aizen.Bff.AdminPanel.Application.Finance.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Finance.Query.GetFinancialSummaryReportBff;

/// <summary>
/// BFF query for the Payment BE-P12 §15 financial summary. Proxies to
/// GET /api/v1/payment/finance/reports/financial-summary. Read-only; no calculation in the BFF.
/// </summary>
public sealed class GetFinancialSummaryReportBffQuery : AizenQuery<FinancialSummaryReportBffDto>
{
    public DateTime From     { get; init; }
    public DateTime To       { get; init; }
    public string   Currency { get; init; } = "TRY";
}
