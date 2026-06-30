using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Query.GetPaymentRefundHistory;

[DocumentationInfo("Get payment refund history BFF query handler",
    "Returns the ordered list of refund records for a given transaction, including reversal metadata.")]
public sealed class GetPaymentRefundHistoryBffQueryHandler
    : AizenQueryHandler<GetPaymentRefundHistoryBffQuery, GetPaymentRefundHistoryBffResponse>
{
    private readonly IAdminPaymentBffRemoteCall _remote;

    public GetPaymentRefundHistoryBffQueryHandler(IAdminPaymentBffRemoteCall remote)
        => _remote = remote;

    public override async Task<GetPaymentRefundHistoryBffResponse> Handle(
        GetPaymentRefundHistoryBffQuery request, CancellationToken ct)
    {
        var records = await _remote.GetRefundHistoryAsync(request.TransactionId, ct);
        return new GetPaymentRefundHistoryBffResponse { Records = records };
    }
}
