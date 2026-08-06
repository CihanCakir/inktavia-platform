using Aizen.Bff.AdminPanel.Application.AdminPayment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.RefundAllocationPolicy;

// ─── Create ──────────────────────────────────────────────────────────────────
public sealed class CreateRefundAllocationPolicyBffCommand : AizenCommand<CreateRefundAllocationPolicyBffResponse>
{
    public CreateRefundAllocationPolicyBffRequest Body { get; init; } = default!;
}
public sealed class CreateRefundAllocationPolicyBffResponse { public RefundAllocationPolicyMutateResultDto Result { get; init; } = default!; }

[DocumentationInfo("Create refund-allocation policy BFF command handler (P10)",
    "Forwards a new refund-allocation policy (POST /admin/refund-allocation-policies). Rule Cause/Mode are string enum names.")]
public sealed class CreateRefundAllocationPolicyBffCommandHandler
    : AizenCommandHandler<CreateRefundAllocationPolicyBffCommand, CreateRefundAllocationPolicyBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public CreateRefundAllocationPolicyBffCommandHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<CreateRefundAllocationPolicyBffResponse?> Handle(CreateRefundAllocationPolicyBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.CreateRefundAllocationPolicyAsync(request.Body, ct) };
}

// ─── Update ──────────────────────────────────────────────────────────────────
public sealed class UpdateRefundAllocationPolicyBffCommand : AizenCommand<UpdateRefundAllocationPolicyBffResponse>
{
    public long                                   Id   { get; init; }
    public UpdateRefundAllocationPolicyBffRequest Body { get; init; } = default!;
}
public sealed class UpdateRefundAllocationPolicyBffResponse { public RefundAllocationPolicyMutateResultDto Result { get; init; } = default!; }

[DocumentationInfo("Update refund-allocation policy BFF command handler (P10)",
    "Forwards a refund-allocation policy update (PUT /admin/refund-allocation-policies/{id}). Id is applied from the route by the module.")]
public sealed class UpdateRefundAllocationPolicyBffCommandHandler
    : AizenCommandHandler<UpdateRefundAllocationPolicyBffCommand, UpdateRefundAllocationPolicyBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public UpdateRefundAllocationPolicyBffCommandHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<UpdateRefundAllocationPolicyBffResponse?> Handle(UpdateRefundAllocationPolicyBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.UpdateRefundAllocationPolicyAsync(request.Id, request.Body, ct) };
}

// ─── Deactivate ──────────────────────────────────────────────────────────────
public sealed class DeactivateRefundAllocationPolicyBffCommand : AizenCommand<DeactivateRefundAllocationPolicyBffResponse>
{
    public long Id { get; init; }
}
public sealed class DeactivateRefundAllocationPolicyBffResponse { public RefundAllocationPolicyMutateResultDto Result { get; init; } = default!; }

[DocumentationInfo("Deactivate refund-allocation policy BFF command handler (P10)",
    "Forwards a deactivate (POST /admin/refund-allocation-policies/{id}/deactivate).")]
public sealed class DeactivateRefundAllocationPolicyBffCommandHandler
    : AizenCommandHandler<DeactivateRefundAllocationPolicyBffCommand, DeactivateRefundAllocationPolicyBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public DeactivateRefundAllocationPolicyBffCommandHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<DeactivateRefundAllocationPolicyBffResponse?> Handle(DeactivateRefundAllocationPolicyBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.DeactivateRefundAllocationPolicyAsync(request.Id, ct) };
}

// ─── Reactivate ──────────────────────────────────────────────────────────────
public sealed class ReactivateRefundAllocationPolicyBffCommand : AizenCommand<ReactivateRefundAllocationPolicyBffResponse>
{
    public long Id { get; init; }
}
public sealed class ReactivateRefundAllocationPolicyBffResponse { public RefundAllocationPolicyMutateResultDto Result { get; init; } = default!; }

[DocumentationInfo("Reactivate refund-allocation policy BFF command handler (P10)",
    "Forwards a reactivate (POST /admin/refund-allocation-policies/{id}/reactivate). A RefundAllocationPolicyConflict " +
    "surfaces through the envelope (fail-loud) when reactivation would overlap an active policy for the currency.")]
public sealed class ReactivateRefundAllocationPolicyBffCommandHandler
    : AizenCommandHandler<ReactivateRefundAllocationPolicyBffCommand, ReactivateRefundAllocationPolicyBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public ReactivateRefundAllocationPolicyBffCommandHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<ReactivateRefundAllocationPolicyBffResponse?> Handle(ReactivateRefundAllocationPolicyBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.ReactivateRefundAllocationPolicyAsync(request.Id, ct) };
}
