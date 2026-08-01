using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Bff.AdminPanel.Application.AdminFinance.Dto;

/// <summary>BFF mirror of the Payment BE-P12 paged ledger drill-down (audit). Typed throughout (never <c>object</c>).</summary>
public sealed class LedgerEntriesPageBffDto
{
    public List<LedgerEntryBffDto> Items    { get; init; } = new();
    public int                     Total    { get; init; }
    public int                     Page     { get; init; }
    public int                     PageSize { get; init; }
}

/// <summary>A single append-only financial-ledger entry (net-of-reversals is applied in the summary, not here).</summary>
public sealed class LedgerEntryBffDto
{
    public long              Id                { get; init; }
    public string            EntryCode         { get; init; } = string.Empty;
    public LedgerAccountLine AccountLine       { get; init; }
    public LedgerEntryNature Nature            { get; init; }
    public decimal           Amount            { get; init; }
    public bool              IsReversal        { get; init; }
    public string            CurrencyCode      { get; init; } = "TRY";
    public LedgerSourceType  SourceType        { get; init; }
    public long              SourceRef         { get; init; }
    public long?             TransactionId     { get; init; }
    public long?             ProviderProfileId { get; init; }
    public long?             CustomerProfileId { get; init; }
    public DateTime          OccurredAtUtc     { get; init; }
}
