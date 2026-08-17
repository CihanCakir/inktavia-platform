using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Application.Queries.GetFinancialSummaryReport;

/// <summary>
/// BE-P12 §15 — period financial summary over [From, To) for a currency: revenue total (excl. VAT liability), expense total,
/// and <b>NetMarketplaceContribution = Σ revenue − Σ (payment + refund + chargeback + discount) expenses</b>, plus the
/// per-line breakdown. VAT liability + provider-funded discount are shown separately (never in the Inktavia P&amp;L).
/// </summary>
public sealed class GetFinancialSummaryReportQuery : AizenQuery<FinancialSummaryReportDto>
{
    public required DateTime From     { get; init; }
    public required DateTime To       { get; init; }
    public          string   Currency { get; init; } = "TRY";
}

public sealed record LedgerLineBreakdownDto(
    LedgerAccountLine AccountLine,
    LedgerEntryNature Nature,
    decimal           Total,        // net of reversals (contra entries subtracted)
    int               EntryCount);

public sealed record FinancialSummaryReportDto
{
    public required DateTime From { get; init; }
    public required DateTime To   { get; init; }
    public required string   Currency { get; init; }

    /// <summary>Σ Revenue (net of reversals), EXCLUDING the VAT liability (§19.17).</summary>
    public decimal RevenueTotal { get; init; }
    /// <summary>Σ Expense (net of reversals) — payment/refund/chargeback/platform-funded-discount + expected/actual.</summary>
    public decimal ExpenseTotal { get; init; }
    /// <summary>§15 formula: RevenueTotal − ExpenseTotal (VAT liability + provider-funded discount excluded).</summary>
    public decimal NetMarketplaceContribution { get; init; }

    /// <summary>Shown SEPARATELY (not in the Inktavia P&amp;L): platform-fee VAT collected on behalf of the tax authority.</summary>
    public decimal VatLiabilityTotal { get; init; }
    /// <summary>Shown SEPARATELY (§19.17 memo): provider-funded customer discount — funded by the provider, NOT an Inktavia expense.</summary>
    public decimal ProviderFundedDiscountTotal { get; init; }

    public required IReadOnlyList<LedgerLineBreakdownDto> Breakdown { get; init; }
}
