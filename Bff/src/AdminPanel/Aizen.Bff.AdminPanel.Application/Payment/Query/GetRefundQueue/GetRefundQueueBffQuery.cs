using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetRefundQueue;


// ─── Refund queue ────────────────────────────────────────────────────────────
public sealed class GetRefundQueueBffQuery : AizenQuery<GetRefundQueueBffResponse>
{
    public RefundCause?             Cause        { get; init; }
    public ReleaseState?            ReleaseState { get; init; }
    public TransactionRefundStatus? Status       { get; init; }
    public int                      Page         { get; init; } = 1;
    public int                      PageSize     { get; init; } = 20;
}
public sealed class GetRefundQueueBffResponse { public RefundQueuePagedDto? Result { get; init; } }
