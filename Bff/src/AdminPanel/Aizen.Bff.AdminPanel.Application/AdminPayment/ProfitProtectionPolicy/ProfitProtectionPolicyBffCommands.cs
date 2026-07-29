using Aizen.Bff.AdminPanel.Application.AdminPayment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.ProfitProtectionPolicy;

// ─── Create ──────────────────────────────────────────────────────────────────
public sealed class CreateProfitProtectionPolicyBffCommand : AizenCommand<CreateProfitProtectionPolicyBffResponse>
{
    public CreateProfitProtectionPolicyBffRequest Body { get; init; } = default!;
}
public sealed class CreateProfitProtectionPolicyBffResponse { public ProfitProtectionPolicyCreateBffResult Result { get; init; } = default!; }

[DocumentationInfo("Create profit-protection policy BFF command handler (BE-P5)",
    "Forwards a new profit-protection policy (POST /profit-protection/policies). ProfitProtectionPolicyConflict/Invalid surfaces through the envelope.")]
public sealed class CreateProfitProtectionPolicyBffCommandHandler
    : AizenCommandHandler<CreateProfitProtectionPolicyBffCommand, CreateProfitProtectionPolicyBffResponse>
{
    private readonly IAdminPaymentBffRemoteCall _payment;
    public CreateProfitProtectionPolicyBffCommandHandler(IAdminPaymentBffRemoteCall payment) => _payment = payment;

    public override async Task<CreateProfitProtectionPolicyBffResponse?> Handle(CreateProfitProtectionPolicyBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.CreateProfitProtectionPolicyAsync(request.Body, ct) };
}

// ─── Update ──────────────────────────────────────────────────────────────────
public sealed class UpdateProfitProtectionPolicyBffCommand : AizenCommand<UpdateProfitProtectionPolicyBffResponse>
{
    public long                                   Id   { get; init; }
    public UpdateProfitProtectionPolicyBffRequest Body { get; init; } = default!;
}
public sealed class UpdateProfitProtectionPolicyBffResponse { public ProfitProtectionPolicyMutateBffResult Result { get; init; } = default!; }

[DocumentationInfo("Update profit-protection policy BFF command handler (BE-P5)",
    "Forwards a policy update (PUT /profit-protection/policies/{id}). Conflict/Invalid surfaces through the envelope.")]
public sealed class UpdateProfitProtectionPolicyBffCommandHandler
    : AizenCommandHandler<UpdateProfitProtectionPolicyBffCommand, UpdateProfitProtectionPolicyBffResponse>
{
    private readonly IAdminPaymentBffRemoteCall _payment;
    public UpdateProfitProtectionPolicyBffCommandHandler(IAdminPaymentBffRemoteCall payment) => _payment = payment;

    public override async Task<UpdateProfitProtectionPolicyBffResponse?> Handle(UpdateProfitProtectionPolicyBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.UpdateProfitProtectionPolicyAsync(request.Id, request.Body with { Id = request.Id }, ct) };
}

// ─── Deactivate ──────────────────────────────────────────────────────────────
public sealed class DeactivateProfitProtectionPolicyBffCommand : AizenCommand<DeactivateProfitProtectionPolicyBffResponse>
{
    public long Id { get; init; }
}
public sealed class DeactivateProfitProtectionPolicyBffResponse { public ProfitProtectionPolicyMutateBffResult Result { get; init; } = default!; }

[DocumentationInfo("Deactivate profit-protection policy BFF command handler (BE-P5)",
    "Forwards a deactivate (POST /profit-protection/policies/{id}/deactivate).")]
public sealed class DeactivateProfitProtectionPolicyBffCommandHandler
    : AizenCommandHandler<DeactivateProfitProtectionPolicyBffCommand, DeactivateProfitProtectionPolicyBffResponse>
{
    private readonly IAdminPaymentBffRemoteCall _payment;
    public DeactivateProfitProtectionPolicyBffCommandHandler(IAdminPaymentBffRemoteCall payment) => _payment = payment;

    public override async Task<DeactivateProfitProtectionPolicyBffResponse?> Handle(DeactivateProfitProtectionPolicyBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.DeactivateProfitProtectionPolicyAsync(request.Id, ct) };
}
