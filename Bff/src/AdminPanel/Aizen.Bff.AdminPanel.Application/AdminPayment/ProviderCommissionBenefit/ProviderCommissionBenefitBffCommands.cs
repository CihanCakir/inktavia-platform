using Aizen.Bff.AdminPanel.Application.AdminPayment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.ProviderCommissionBenefit;

// ─── Rule: Create ────────────────────────────────────────────────────────────
public sealed class CreateProviderCommissionBenefitRuleBffCommand : AizenCommand<CreateProviderCommissionBenefitRuleBffResponse>
{
    public CreateProviderCommissionBenefitRuleBffRequest Body { get; init; } = default!;
}
public sealed class CreateProviderCommissionBenefitRuleBffResponse { public ProviderCommissionBenefitRuleCreateBffResult Result { get; init; } = default!; }

[DocumentationInfo("Create provider-commission-benefit rule BFF command handler (BE-P7)",
    "Forwards a new benefit rule (POST /commission-benefits/rules). Conflict/Invalid (e.g. both Stackable and Exclusive) surfaces through the envelope.")]
public sealed class CreateProviderCommissionBenefitRuleBffCommandHandler
    : AizenCommandHandler<CreateProviderCommissionBenefitRuleBffCommand, CreateProviderCommissionBenefitRuleBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public CreateProviderCommissionBenefitRuleBffCommandHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<CreateProviderCommissionBenefitRuleBffResponse?> Handle(CreateProviderCommissionBenefitRuleBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.CreateProviderCommissionBenefitRuleAsync(request.Body, ct) };
}

// ─── Rule: Update ────────────────────────────────────────────────────────────
public sealed class UpdateProviderCommissionBenefitRuleBffCommand : AizenCommand<UpdateProviderCommissionBenefitRuleBffResponse>
{
    public long                                            Id   { get; init; }
    public UpdateProviderCommissionBenefitRuleBffRequest   Body { get; init; } = default!;
}
public sealed class UpdateProviderCommissionBenefitRuleBffResponse { public ProviderCommissionBenefitRuleMutateBffResult Result { get; init; } = default!; }

[DocumentationInfo("Update provider-commission-benefit rule BFF command handler (BE-P7)",
    "Forwards a benefit rule update (PUT /commission-benefits/rules/{id}).")]
public sealed class UpdateProviderCommissionBenefitRuleBffCommandHandler
    : AizenCommandHandler<UpdateProviderCommissionBenefitRuleBffCommand, UpdateProviderCommissionBenefitRuleBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public UpdateProviderCommissionBenefitRuleBffCommandHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<UpdateProviderCommissionBenefitRuleBffResponse?> Handle(UpdateProviderCommissionBenefitRuleBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.UpdateProviderCommissionBenefitRuleAsync(request.Id, request.Body with { Id = request.Id }, ct) };
}

// ─── Rule: Deactivate ────────────────────────────────────────────────────────
public sealed class DeactivateProviderCommissionBenefitRuleBffCommand : AizenCommand<DeactivateProviderCommissionBenefitRuleBffResponse>
{
    public long Id { get; init; }
}
public sealed class DeactivateProviderCommissionBenefitRuleBffResponse { public ProviderCommissionBenefitRuleMutateBffResult Result { get; init; } = default!; }

[DocumentationInfo("Deactivate provider-commission-benefit rule BFF command handler (BE-P7)",
    "Forwards a deactivate (POST /commission-benefits/rules/{id}/deactivate).")]
public sealed class DeactivateProviderCommissionBenefitRuleBffCommandHandler
    : AizenCommandHandler<DeactivateProviderCommissionBenefitRuleBffCommand, DeactivateProviderCommissionBenefitRuleBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public DeactivateProviderCommissionBenefitRuleBffCommandHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<DeactivateProviderCommissionBenefitRuleBffResponse?> Handle(DeactivateProviderCommissionBenefitRuleBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.DeactivateProviderCommissionBenefitRuleAsync(request.Id, ct) };
}

// ─── Rule: Reactivate ────────────────────────────────────────────────────────
public sealed class ReactivateProviderCommissionBenefitRuleBffCommand : AizenCommand<ReactivateProviderCommissionBenefitRuleBffResponse>
{
    public long Id { get; init; }
}
public sealed class ReactivateProviderCommissionBenefitRuleBffResponse { public ProviderCommissionBenefitRuleMutateBffResult Result { get; init; } = default!; }

[DocumentationInfo("Reactivate provider-commission-benefit rule BFF command handler (BE-P7)",
    "Forwards a reactivate (POST /commission-benefits/rules/{id}/reactivate). Overlap re-check → 5070 conflict via the envelope.")]
public sealed class ReactivateProviderCommissionBenefitRuleBffCommandHandler
    : AizenCommandHandler<ReactivateProviderCommissionBenefitRuleBffCommand, ReactivateProviderCommissionBenefitRuleBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public ReactivateProviderCommissionBenefitRuleBffCommandHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<ReactivateProviderCommissionBenefitRuleBffResponse?> Handle(ReactivateProviderCommissionBenefitRuleBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.ReactivateProviderCommissionBenefitRuleAsync(request.Id, ct) };
}

// ─── Entitlement: Grant ──────────────────────────────────────────────────────
public sealed class GrantProviderCommissionBenefitEntitlementBffCommand : AizenCommand<GrantProviderCommissionBenefitEntitlementBffResponse>
{
    public GrantProviderCommissionBenefitEntitlementBffRequest Body { get; init; } = default!;
}
public sealed class GrantProviderCommissionBenefitEntitlementBffResponse { public GrantEntitlementBffResult Result { get; init; } = default!; }

[DocumentationInfo("Grant provider-commission-benefit entitlement BFF command handler (BE-P7)",
    "Forwards an entitlement grant (POST /commission-benefits/entitlements).")]
public sealed class GrantProviderCommissionBenefitEntitlementBffCommandHandler
    : AizenCommandHandler<GrantProviderCommissionBenefitEntitlementBffCommand, GrantProviderCommissionBenefitEntitlementBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public GrantProviderCommissionBenefitEntitlementBffCommandHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<GrantProviderCommissionBenefitEntitlementBffResponse?> Handle(GrantProviderCommissionBenefitEntitlementBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.GrantProviderCommissionBenefitEntitlementAsync(request.Body, ct) };
}

// ─── Entitlement: Revoke ─────────────────────────────────────────────────────
public sealed class RevokeProviderCommissionBenefitEntitlementBffCommand : AizenCommand<RevokeProviderCommissionBenefitEntitlementBffResponse>
{
    public long Id { get; init; }
}
public sealed class RevokeProviderCommissionBenefitEntitlementBffResponse { public RevokeEntitlementBffResult Result { get; init; } = default!; }

[DocumentationInfo("Revoke provider-commission-benefit entitlement BFF command handler (BE-P7)",
    "Forwards an entitlement revoke (POST /commission-benefits/entitlements/{id}/revoke).")]
public sealed class RevokeProviderCommissionBenefitEntitlementBffCommandHandler
    : AizenCommandHandler<RevokeProviderCommissionBenefitEntitlementBffCommand, RevokeProviderCommissionBenefitEntitlementBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public RevokeProviderCommissionBenefitEntitlementBffCommandHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<RevokeProviderCommissionBenefitEntitlementBffResponse?> Handle(RevokeProviderCommissionBenefitEntitlementBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.RevokeProviderCommissionBenefitEntitlementAsync(request.Id, ct) };
}
