using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.CustomerDiscount;

// ─── CustomerDiscountRule: Create ────────────────────────────────────────────
public sealed class CreateCustomerDiscountRuleBffCommand : AizenCommand<CreateCustomerDiscountRuleBffResponse>
{
    public CreateCustomerDiscountRuleBffRequest Body { get; init; } = default!;
}
public sealed class CreateCustomerDiscountRuleBffResponse { public CustomerDiscountRuleCreateBffResult Result { get; init; } = default!; }

[DocumentationInfo("Create customer-discount rule BFF command handler (BE-P6)",
    "Forwards a new customer-discount rule (POST /customer-discounts/rules). CustomerDiscountRuleConflict/Invalid surfaces through the envelope.")]
public sealed class CreateCustomerDiscountRuleBffCommandHandler
    : AizenCommandHandler<CreateCustomerDiscountRuleBffCommand, CreateCustomerDiscountRuleBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public CreateCustomerDiscountRuleBffCommandHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<CreateCustomerDiscountRuleBffResponse?> Handle(CreateCustomerDiscountRuleBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.CreateCustomerDiscountRuleAsync(request.Body, ct) };
}

// ─── CustomerDiscountRule: Update ────────────────────────────────────────────
public sealed class UpdateCustomerDiscountRuleBffCommand : AizenCommand<UpdateCustomerDiscountRuleBffResponse>
{
    public long                                 Id   { get; init; }
    public UpdateCustomerDiscountRuleBffRequest Body { get; init; } = default!;
}
public sealed class UpdateCustomerDiscountRuleBffResponse { public CustomerDiscountRuleMutateBffResult Result { get; init; } = default!; }

[DocumentationInfo("Update customer-discount rule BFF command handler (BE-P6)",
    "Forwards a customer-discount rule update (PUT /customer-discounts/rules/{id}). Conflict/Invalid surfaces through the envelope.")]
public sealed class UpdateCustomerDiscountRuleBffCommandHandler
    : AizenCommandHandler<UpdateCustomerDiscountRuleBffCommand, UpdateCustomerDiscountRuleBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public UpdateCustomerDiscountRuleBffCommandHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<UpdateCustomerDiscountRuleBffResponse?> Handle(UpdateCustomerDiscountRuleBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.UpdateCustomerDiscountRuleAsync(request.Id, request.Body with { Id = request.Id }, ct) };
}

// ─── CustomerDiscountRule: Deactivate ────────────────────────────────────────
public sealed class DeactivateCustomerDiscountRuleBffCommand : AizenCommand<DeactivateCustomerDiscountRuleBffResponse>
{
    public long Id { get; init; }
}
public sealed class DeactivateCustomerDiscountRuleBffResponse { public CustomerDiscountRuleMutateBffResult Result { get; init; } = default!; }

[DocumentationInfo("Deactivate customer-discount rule BFF command handler (BE-P6)",
    "Forwards a deactivate (POST /customer-discounts/rules/{id}/deactivate).")]
public sealed class DeactivateCustomerDiscountRuleBffCommandHandler
    : AizenCommandHandler<DeactivateCustomerDiscountRuleBffCommand, DeactivateCustomerDiscountRuleBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public DeactivateCustomerDiscountRuleBffCommandHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<DeactivateCustomerDiscountRuleBffResponse?> Handle(DeactivateCustomerDiscountRuleBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.DeactivateCustomerDiscountRuleAsync(request.Id, ct) };
}

// ─── CustomerDiscountRule: Reactivate ────────────────────────────────────────
public sealed class ReactivateCustomerDiscountRuleBffCommand : AizenCommand<ReactivateCustomerDiscountRuleBffResponse>
{
    public long Id { get; init; }
}
public sealed class ReactivateCustomerDiscountRuleBffResponse { public CustomerDiscountRuleMutateBffResult Result { get; init; } = default!; }

[DocumentationInfo("Reactivate customer-discount rule BFF command handler (BE-P6)",
    "Forwards a reactivate (POST /customer-discounts/rules/{id}/reactivate). The module re-runs the specificity/overlap " +
    "guard, so CustomerDiscountRuleConflict may surface through the envelope.")]
public sealed class ReactivateCustomerDiscountRuleBffCommandHandler
    : AizenCommandHandler<ReactivateCustomerDiscountRuleBffCommand, ReactivateCustomerDiscountRuleBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public ReactivateCustomerDiscountRuleBffCommandHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<ReactivateCustomerDiscountRuleBffResponse?> Handle(ReactivateCustomerDiscountRuleBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.ReactivateCustomerDiscountRuleAsync(request.Id, ct) };
}

// ─── CustomerBenefitBudgetPolicy: Create (per plan) ──────────────────────────
public sealed class CreateCustomerBenefitBudgetPolicyBffCommand : AizenCommand<CreateCustomerBenefitBudgetPolicyBffResponse>
{
    public CreateCustomerBenefitBudgetPolicyBffRequest Body { get; init; } = default!;
}
public sealed class CreateCustomerBenefitBudgetPolicyBffResponse { public CustomerBenefitBudgetPolicyCreateBffResult Result { get; init; } = default!; }

[DocumentationInfo("Create customer-benefit budget policy BFF command handler (BE-P6)",
    "Forwards a new per-plan benefit-budget policy (POST /benefit-budget/policies). CustomerBenefitBudgetPolicyConflict surfaces through the envelope.")]
public sealed class CreateCustomerBenefitBudgetPolicyBffCommandHandler
    : AizenCommandHandler<CreateCustomerBenefitBudgetPolicyBffCommand, CreateCustomerBenefitBudgetPolicyBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public CreateCustomerBenefitBudgetPolicyBffCommandHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<CreateCustomerBenefitBudgetPolicyBffResponse?> Handle(CreateCustomerBenefitBudgetPolicyBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.CreateCustomerBenefitBudgetPolicyAsync(request.Body, ct) };
}
