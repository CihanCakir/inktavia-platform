using Aizen.Bff.AdminPanel.Application.AdminPayment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.PlatformFeeRule;

// ─── Create ──────────────────────────────────────────────────────────────────
public sealed class CreatePlatformFeeRuleBffCommand : AizenCommand<CreatePlatformFeeRuleBffResponse>
{
    public CreatePlatformFeeRuleBffRequest Body { get; init; } = default!;
}
public sealed class CreatePlatformFeeRuleBffResponse { public PlatformFeeRuleCreateBffResult Result { get; init; } = default!; }

[DocumentationInfo("Create platform-fee rule BFF command handler (BE-P3)",
    "Forwards a new platform-fee rule to the Payment module (POST /platform-fee/rules). Model/Priority are string enum " +
    "names parsed by the module. A PlatformFeeRuleConflict/Invalid surfaces through the envelope (fail-loud).")]
public sealed class CreatePlatformFeeRuleBffCommandHandler
    : AizenCommandHandler<CreatePlatformFeeRuleBffCommand, CreatePlatformFeeRuleBffResponse>
{
    private readonly IAdminPaymentBffRemoteCall _payment;
    public CreatePlatformFeeRuleBffCommandHandler(IAdminPaymentBffRemoteCall payment) => _payment = payment;

    public override async Task<CreatePlatformFeeRuleBffResponse?> Handle(CreatePlatformFeeRuleBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.CreatePlatformFeeRuleAsync(request.Body, ct) };
}

// ─── Update ──────────────────────────────────────────────────────────────────
public sealed class UpdatePlatformFeeRuleBffCommand : AizenCommand<UpdatePlatformFeeRuleBffResponse>
{
    public long                          Id   { get; init; }
    public UpdatePlatformFeeRuleBffRequest Body { get; init; } = default!;
}
public sealed class UpdatePlatformFeeRuleBffResponse { public PlatformFeeRuleMutateBffResult Result { get; init; } = default!; }

[DocumentationInfo("Update platform-fee rule BFF command handler (BE-P3)",
    "Forwards a platform-fee rule update (PUT /platform-fee/rules/{id}). Conflict/Invalid surfaces through the envelope.")]
public sealed class UpdatePlatformFeeRuleBffCommandHandler
    : AizenCommandHandler<UpdatePlatformFeeRuleBffCommand, UpdatePlatformFeeRuleBffResponse>
{
    private readonly IAdminPaymentBffRemoteCall _payment;
    public UpdatePlatformFeeRuleBffCommandHandler(IAdminPaymentBffRemoteCall payment) => _payment = payment;

    public override async Task<UpdatePlatformFeeRuleBffResponse?> Handle(UpdatePlatformFeeRuleBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.UpdatePlatformFeeRuleAsync(request.Id, request.Body with { Id = request.Id }, ct) };
}

// ─── Deactivate ──────────────────────────────────────────────────────────────
public sealed class DeactivatePlatformFeeRuleBffCommand : AizenCommand<DeactivatePlatformFeeRuleBffResponse>
{
    public long Id { get; init; }
}
public sealed class DeactivatePlatformFeeRuleBffResponse { public PlatformFeeRuleMutateBffResult Result { get; init; } = default!; }

[DocumentationInfo("Deactivate platform-fee rule BFF command handler (BE-P3)",
    "Forwards a deactivate (POST /platform-fee/rules/{id}/deactivate).")]
public sealed class DeactivatePlatformFeeRuleBffCommandHandler
    : AizenCommandHandler<DeactivatePlatformFeeRuleBffCommand, DeactivatePlatformFeeRuleBffResponse>
{
    private readonly IAdminPaymentBffRemoteCall _payment;
    public DeactivatePlatformFeeRuleBffCommandHandler(IAdminPaymentBffRemoteCall payment) => _payment = payment;

    public override async Task<DeactivatePlatformFeeRuleBffResponse?> Handle(DeactivatePlatformFeeRuleBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.DeactivatePlatformFeeRuleAsync(request.Id, ct) };
}

// ─── Reactivate ──────────────────────────────────────────────────────────────
public sealed class ReactivatePlatformFeeRuleBffCommand : AizenCommand<ReactivatePlatformFeeRuleBffResponse>
{
    public long Id { get; init; }
}
public sealed class ReactivatePlatformFeeRuleBffResponse { public PlatformFeeRuleMutateBffResult Result { get; init; } = default!; }

[DocumentationInfo("Reactivate platform-fee rule BFF command handler (BE-P3)",
    "Forwards a reactivate (POST /platform-fee/rules/{id}/reactivate). A PlatformFeeRuleConflict/NotInactive surfaces " +
    "through the envelope (fail-loud) when reactivation would collide with an active rule.")]
public sealed class ReactivatePlatformFeeRuleBffCommandHandler
    : AizenCommandHandler<ReactivatePlatformFeeRuleBffCommand, ReactivatePlatformFeeRuleBffResponse>
{
    private readonly IAdminPaymentBffRemoteCall _payment;
    public ReactivatePlatformFeeRuleBffCommandHandler(IAdminPaymentBffRemoteCall payment) => _payment = payment;

    public override async Task<ReactivatePlatformFeeRuleBffResponse?> Handle(ReactivatePlatformFeeRuleBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.ReactivatePlatformFeeRuleAsync(request.Id, ct) };
}
