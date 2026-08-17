namespace Aizen.Modules.Payment.Domain.Entities.Reporting;

/// <summary>
/// C1 — one month bucket for the admin dashboard revenue chart: total revenue and the commission portion, in the
/// platform settlement currency (TRY), derived from the append-only financial ledger. A read-model, not persisted;
/// months with no ledger activity are zero-filled so the chart x-axis stays continuous.
/// </summary>
[DocumentationInfo("Monthly revenue/commission point",
    "One (month → revenue, commission) bucket for the admin dashboard revenue chart (C1). Zero-filled for empty months.")]
public sealed class MonthlyRevenueCommissionPoint
{
    /// <summary>First day of the month, UTC.</summary>
    public DateOnly Month { get; set; }
    public decimal Revenue { get; set; }
    public decimal Commission { get; set; }
}
