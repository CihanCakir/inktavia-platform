using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Application.Queries.GetLedgerEntries;

/// <summary>BE-P12 §15 — paged drill-down over the financial ledger (audit) filtered by account line / source / provider / period.</summary>
public sealed class GetLedgerEntriesQuery : AizenQuery<LedgerEntriesPageDto>
{
    public LedgerAccountLine? AccountLine       { get; init; }
    public LedgerSourceType?  SourceType        { get; init; }
    public long?              ProviderProfileId { get; init; }
    public DateTime?          From              { get; init; }
    public DateTime?          To                { get; init; }
    public string?            Currency          { get; init; }
    public int                Page              { get; init; } = 1;
    public int                PageSize          { get; init; } = 50;
}

public sealed record LedgerEntryDto(
    long              Id,
    string            EntryCode,
    LedgerAccountLine AccountLine,
    LedgerEntryNature Nature,
    decimal           Amount,
    bool              IsReversal,
    string            CurrencyCode,
    LedgerSourceType  SourceType,
    long              SourceRef,
    long?             TransactionId,
    long?             ProviderProfileId,
    long?             CustomerProfileId,
    DateTime          OccurredAtUtc);

public sealed record LedgerEntriesPageDto(IReadOnlyList<LedgerEntryDto> Items, int Total, int Page, int PageSize);
