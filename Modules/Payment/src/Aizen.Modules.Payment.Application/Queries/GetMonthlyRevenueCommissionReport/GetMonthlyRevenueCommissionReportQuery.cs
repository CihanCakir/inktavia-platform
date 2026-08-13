using Aizen.Core.CQRS.Message;

namespace Aizen.Modules.Payment.Application.Queries.GetMonthlyRevenueCommissionReport;

/// <summary>
/// C1 — the last <see cref="Months"/> months of platform revenue + commission (settlement currency TRY),
/// oldest→newest, zero-filled, UTC calendar-month buckets. Derived from the append-only financial ledger using the
/// same sign classification as the §15 summary (no recompute).
/// </summary>
public sealed class GetMonthlyRevenueCommissionReportQuery : AizenQuery<List<MonthlyRevenueCommissionDto>>
{
    public int Months { get; init; } = 12;
}

/// <summary>One month bucket for the admin dashboard revenue chart (C1).</summary>
public sealed record MonthlyRevenueCommissionDto
{
    /// <summary>Month bucket, "yyyy-MM" (UTC).</summary>
    public required string Month { get; init; }

    /// <summary>Σ revenue-nature ledger lines (net of reversals) for the month.</summary>
    public decimal Revenue { get; init; }

    /// <summary>Σ provider-commission-revenue lines (net of reversals) — the commission portion of Revenue.</summary>
    public decimal Commission { get; init; }
}
