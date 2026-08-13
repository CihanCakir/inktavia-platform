using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetMonthlyRevenueCommissionReport;

[DocumentationInfo("GetMonthlyRevenueCommissionReportQueryHandler (C1)",
    "Maps the ledger-derived monthly (revenue, commission) read-model to the yyyy-MM chart DTO. Reuses the ledger sign classification; no recompute.")]
public sealed class GetMonthlyRevenueCommissionReportQueryHandler
    : AizenQueryHandler<GetMonthlyRevenueCommissionReportQuery, List<MonthlyRevenueCommissionDto>>
{
    private readonly IFinancialLedgerRepository _ledger;

    public GetMonthlyRevenueCommissionReportQueryHandler(IFinancialLedgerRepository ledger) => _ledger = ledger;

    public override async Task<List<MonthlyRevenueCommissionDto>?> Handle(
        GetMonthlyRevenueCommissionReportQuery request, CancellationToken ct)
    {
        var points = await _ledger.GetMonthlyRevenueCommissionAsync(request.Months, ct);
        return points
            .Select(p => new MonthlyRevenueCommissionDto
            {
                Month = p.Month.ToString("yyyy-MM"),
                Revenue = p.Revenue,
                Commission = p.Commission,
            })
            .ToList();
    }
}
