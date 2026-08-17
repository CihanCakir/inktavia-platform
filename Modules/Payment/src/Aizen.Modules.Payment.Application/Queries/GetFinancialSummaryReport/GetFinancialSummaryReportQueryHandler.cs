using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.Reporting;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Aizen.Modules.Payment.Domain.Money;

namespace Aizen.Modules.Payment.Application.Queries.GetFinancialSummaryReport;

[DocumentationInfo("GetFinancialSummaryReportQueryHandler (BE-P12)",
    "Aggregates the append-only financial ledger over a period into revenue/expense totals + NetMarketplaceContribution " +
    "(§15 formula) + per-line breakdown. VAT liability + provider-funded discount are excluded from the Inktavia P&L and " +
    "surfaced separately (§19.17). Derived entirely from the immutable ledger — no recompute.")]
public sealed class GetFinancialSummaryReportQueryHandler
    : AizenQueryHandler<GetFinancialSummaryReportQuery, FinancialSummaryReportDto>
{
    private readonly IFinancialLedgerRepository _ledger;

    public GetFinancialSummaryReportQueryHandler(IFinancialLedgerRepository ledger) => _ledger = ledger;

    public override async Task<FinancialSummaryReportDto?> Handle(
        GetFinancialSummaryReportQuery request, CancellationToken ct)
    {
        var entries = await _ledger.GetForPeriodAsync(request.From, request.To, request.Currency, ct);

        // Per-line breakdown: sum each account line NET of reversals (a contra entry subtracts).
        var breakdown = entries
            .GroupBy(e => e.AccountLine)
            .Select(g => new LedgerLineBreakdownDto(
                AccountLine: g.Key,
                Nature:      FinancialLedgerEntryEntity.NatureOf(g.Key),
                Total:       MoneyMath.Round(g.Sum(e => e.IsReversal ? -e.Amount : e.Amount)),
                EntryCount:  g.Count()))
            .OrderBy(b => b.AccountLine)
            .ToList();

        decimal SumNature(LedgerEntryNature nature) =>
            MoneyMath.Round(entries.Where(e => e.Nature == nature).Sum(e => e.IsReversal ? -e.Amount : e.Amount));

        var revenueTotal = SumNature(LedgerEntryNature.Revenue);
        var expenseTotal = SumNature(LedgerEntryNature.Expense);
        var vatLiability  = SumNature(LedgerEntryNature.Liability);

        // §19.17 — provider-funded customer discount is a MEMO, surfaced separately (NOT in the Inktavia P&L).
        var providerFundedDiscount = MoneyMath.Round(entries
            .Where(e => e.AccountLine == LedgerAccountLine.ProviderFundedCustomerDiscount)
            .Sum(e => e.IsReversal ? -e.Amount : e.Amount));

        // §15 — NetMarketplaceContribution = Σ revenue − Σ (payment + refund + chargeback + discount) expenses.
        // VAT liability + provider-funded discount are excluded by construction (Liability / Memo natures).
        var netContribution = MoneyMath.Round(revenueTotal - expenseTotal);

        return new FinancialSummaryReportDto
        {
            From                        = request.From,
            To                          = request.To,
            Currency                    = request.Currency.ToUpperInvariant(),
            RevenueTotal                = revenueTotal,
            ExpenseTotal                = expenseTotal,
            NetMarketplaceContribution  = netContribution,
            VatLiabilityTotal           = vatLiability,
            ProviderFundedDiscountTotal = providerFundedDiscount,
            Breakdown                   = breakdown,
        };
    }
}
