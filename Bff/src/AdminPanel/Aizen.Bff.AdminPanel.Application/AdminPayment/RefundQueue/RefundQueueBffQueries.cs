using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.RefundQueue;

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

// ─── Chargeback queue ────────────────────────────────────────────────────────
public sealed class GetChargebackQueueBffQuery : AizenQuery<GetChargebackQueueBffResponse>
{
    public int Page     { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
public sealed class GetChargebackQueueBffResponse { public ChargebackQueuePagedDto? Result { get; init; } }

[DocumentationInfo("Get chargeback queue BFF query handler (P10)",
    "Paged chargeback queue (GET /admin/chargeback-queue). Read-only.")]
public sealed class GetChargebackQueueBffQueryHandler
    : AizenQueryHandler<GetChargebackQueueBffQuery, GetChargebackQueueBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public GetChargebackQueueBffQueryHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<GetChargebackQueueBffResponse?> Handle(GetChargebackQueueBffQuery request, CancellationToken ct)
        => new() { Result = await _payment.GetChargebackQueueAsync(request.Page, request.PageSize, ct) };
}
