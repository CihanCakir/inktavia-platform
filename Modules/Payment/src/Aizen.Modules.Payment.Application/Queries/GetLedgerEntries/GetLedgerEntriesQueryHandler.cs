using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetLedgerEntries;

[DocumentationInfo("GetLedgerEntriesQueryHandler (BE-P12)",
    "Paged audit drill-down over the append-only financial ledger with optional account-line / source / provider / period filters.")]
public sealed class GetLedgerEntriesQueryHandler
    : AizenQueryHandler<GetLedgerEntriesQuery, LedgerEntriesPageDto>
{
    private readonly IFinancialLedgerRepository _ledger;

    public GetLedgerEntriesQueryHandler(IFinancialLedgerRepository ledger) => _ledger = ledger;

    public override async Task<LedgerEntriesPageDto?> Handle(
        GetLedgerEntriesQuery request, CancellationToken ct)
    {
        var page     = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 500 ? 50 : request.PageSize;

        var (items, total) = await _ledger.GetPagedAsync(
            request.AccountLine, request.SourceType, request.ProviderProfileId,
            request.From, request.To, request.Currency, (page - 1) * pageSize, pageSize, ct);

        var dtos = items.Select(e => new LedgerEntryDto(
            e.Id, e.EntryCode, e.AccountLine, e.Nature, e.Amount, e.IsReversal, e.CurrencyCode,
            e.SourceType, e.SourceRef, e.TransactionId, e.ProviderProfileId, e.CustomerProfileId, e.OccurredAtUtc)).ToList();

        return new LedgerEntriesPageDto(dtos, total, page, pageSize);
    }
}
