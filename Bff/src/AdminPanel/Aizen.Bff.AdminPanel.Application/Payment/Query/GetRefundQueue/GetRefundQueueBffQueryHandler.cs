using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetRefundQueue;

[DocumentationInfo("Get refund queue BFF query handler (P10)",
    "Paged refund queue with optional cause/release-state/status filters (GET /admin/refund-queue). Read-only.")]
public sealed class GetRefundQueueBffQueryHandler
    : AizenQueryHandler<GetRefundQueueBffQuery, GetRefundQueueBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public GetRefundQueueBffQueryHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<GetRefundQueueBffResponse?> Handle(GetRefundQueueBffQuery request, CancellationToken ct)
        => new() { Result = await _payment.GetRefundQueueAsync(
            request.Cause, request.ReleaseState, request.Status, request.Page, request.PageSize, ct) };
}
