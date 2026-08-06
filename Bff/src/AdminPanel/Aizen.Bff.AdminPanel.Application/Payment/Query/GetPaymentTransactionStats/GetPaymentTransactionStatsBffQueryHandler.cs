using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetPaymentTransactionStats;

[DocumentationInfo("Get payment transaction stats BFF query handler",
    "Returns transaction ledger KPI summary: net liquidity, pending clearances, operational burn, and fleet ROI with change deltas.")]
public sealed class GetPaymentTransactionStatsBffQueryHandler
    : AizenQueryHandler<GetPaymentTransactionStatsBffQuery, GetPaymentTransactionStatsBffResponse>
{
    private readonly IPaymentRemoteCall _remote;

    public GetPaymentTransactionStatsBffQueryHandler(IPaymentRemoteCall remote)
        => _remote = remote;

    public override async Task<GetPaymentTransactionStatsBffResponse> Handle(
        GetPaymentTransactionStatsBffQuery request, CancellationToken ct)
    {
        var stats = await _remote.GetTransactionStatsAsync(ct);
        return new GetPaymentTransactionStatsBffResponse { Stats = stats };
    }
}
