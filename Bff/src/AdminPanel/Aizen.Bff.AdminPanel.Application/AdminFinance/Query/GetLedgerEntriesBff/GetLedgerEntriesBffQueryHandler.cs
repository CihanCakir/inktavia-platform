using Aizen.Bff.AdminPanel.Application.AdminFinance.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminFinance.Query.GetLedgerEntriesBff;

[DocumentationInfo("Get ledger entries BFF query handler (BE-P12)",
    "Proxies the admin ledger drill-down request to the Payment finance endpoint. Returns a paged, filtered view " +
    "over the append-only financial ledger (audit). No financial calculations are performed in the BFF.")]
public sealed class GetLedgerEntriesBffQueryHandler
    : AizenQueryHandler<GetLedgerEntriesBffQuery, LedgerEntriesPageBffDto>
{
    private readonly IAdminPaymentBffRemoteCall _remote;

    public GetLedgerEntriesBffQueryHandler(IAdminPaymentBffRemoteCall remote) => _remote = remote;

    public override async Task<LedgerEntriesPageBffDto> Handle(
        GetLedgerEntriesBffQuery request, CancellationToken ct)
        => await _remote.GetLedgerEntriesAsync(
            request.AccountLine,
            request.SourceType,
            request.ProviderProfileId,
            request.From,
            request.To,
            request.Currency,
            request.Page,
            request.PageSize,
            ct);
}
