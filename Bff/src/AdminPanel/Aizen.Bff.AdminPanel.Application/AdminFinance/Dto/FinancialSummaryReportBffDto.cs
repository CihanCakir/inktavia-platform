using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Bff.AdminPanel.Application.AdminFinance.Dto;

/// <summary>
/// BFF mirror of the Payment BE-P12 §15 period financial summary (the module DTO lives in the Payment Application
/// project, not Abstraction, so the BFF carries its own typed shape). NetMarketplaceContribution = Σ revenue −
/// Σ expenses; VAT liability + provider-funded discount are surfaced SEPARATELY (never in the Inktavia P&amp;L, §19.17).
/// </summary>
public sealed class FinancialSummaryReportBffDto
{
    public DateTime From     { get; init; }
    public DateTime To       { get; init; }
    public string   Currency { get; init; } = "TRY";

    public decimal RevenueTotal               { get; init; }
    public decimal ExpenseTotal               { get; init; }
    public decimal NetMarketplaceContribution { get; init; }
    public decimal VatLiabilityTotal          { get; init; }
    public decimal ProviderFundedDiscountTotal { get; init; }

    public List<LedgerLineBreakdownBffDto> Breakdown { get; init; } = new();
}

/// <summary>Per-account-line rollup (net of reversals) for the summary breakdown.</summary>
public sealed class LedgerLineBreakdownBffDto
{
    public LedgerAccountLine AccountLine { get; init; }
    public LedgerEntryNature Nature      { get; init; }
    public decimal           Total       { get; init; }
    public int               EntryCount  { get; init; }
}
