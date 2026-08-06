using Aizen.Bff.AdminPanel.Application.Finance.Dto;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Bff.AdminPanel.Application.Finance.Query.GetLedgerEntriesBff;

/// <summary>
/// BFF query for the Payment BE-P12 ledger drill-down (audit). Proxies to
/// GET /api/v1/payment/finance/reports/ledger-entries. Paged, filter by account line / source / provider / period.
/// </summary>
public sealed class GetLedgerEntriesBffQuery : AizenQuery<LedgerEntriesPageBffDto>
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
