using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Dto;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.RefundAllocation;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Commands.RefundAllocationPolicyAdmin;

/// <summary>One per-cause rule input for creating a refund-allocation policy.</summary>
public sealed class RefundAllocationPolicyRuleInput
{
    public RefundCause            Cause                  { get; init; }
    public PlatformFeeRefundMode  Mode                   { get; init; }
    public decimal?               FixedPlatformFeeAmount { get; init; }
}

// ─── Create (MVP §7.2 defaults when no rules supplied) ───────────────────────
public sealed class CreateRefundAllocationPolicyCommand : AizenCommand<RefundAllocationPolicyMutateResultDto>
{
    public string    CurrencyCode         { get; init; } = "TRY";
    public decimal   NegativeBalanceLimit { get; init; } = 50000m;
    public DateTime  EffectiveFrom        { get; init; }
    public DateTime? EffectiveTo          { get; init; }
    public string?   PolicyName           { get; init; }
    public string?   Notes                { get; init; }
    public List<RefundAllocationPolicyRuleInput>? Rules { get; init; }
}

public sealed class CreateRefundAllocationPolicyCommandHandler
    : AizenCommandHandler<CreateRefundAllocationPolicyCommand, RefundAllocationPolicyMutateResultDto>
{
    private readonly IRefundAllocationPolicyRepository _policies;
    public CreateRefundAllocationPolicyCommandHandler(IRefundAllocationPolicyRepository policies) => _policies = policies;

    public override async Task<RefundAllocationPolicyMutateResultDto?> Handle(CreateRefundAllocationPolicyCommand r, CancellationToken ct)
    {
        var effFrom = r.EffectiveFrom == default ? DateTime.UtcNow : r.EffectiveFrom.ToUniversalTime();
        var policy = RefundAllocationPolicyEntity.Create(
            r.CurrencyCode, r.NegativeBalanceLimit, effFrom, r.EffectiveTo?.ToUniversalTime(),
            policyCode: null, policyName: r.PolicyName, notes: r.Notes);

        if (r.Rules is { Count: > 0 })
            foreach (var rule in r.Rules) policy.AddRule(rule.Cause, rule.Mode, rule.FixedPlatformFeeAmount);
        else
            SeedDefaultRules(policy);   // MVP §7.2 table

        // §7.2 fail-loud: no other active policy may cover the same currency/window.
        var existingActive = (await _policies.GetAllAsync(ct)).Where(p => p.IsActive && p.CurrencyCode == policy.CurrencyCode);
        if (RefundAllocationPolicyResolver.FindOverlappingConflict(policy, existingActive) is not null)
            throw new AizenBusinessException((int)PaymentErrorCode.RefundAllocationPolicyConflict,
                $"An active refund-allocation policy already overlaps for currency {policy.CurrencyCode}.");

        await _policies.AddAsync(policy, ct);
        await _policies.SaveChangesAsync(ct);
        return new RefundAllocationPolicyMutateResultDto(policy.Id, policy.PolicyCode);
    }

    private static void SeedDefaultRules(RefundAllocationPolicyEntity p)
    {
        p.AddRule(RefundCause.ProviderCancelled,                 PlatformFeeRefundMode.Full);
        p.AddRule(RefundCause.TechnicalFailure,                  PlatformFeeRefundMode.Full);
        p.AddRule(RefundCause.DuplicatePayment,                  PlatformFeeRefundMode.Full);
        p.AddRule(RefundCause.CustomerCancelledBeforeWork,       PlatformFeeRefundMode.Full);
        p.AddRule(RefundCause.CustomerCancelledAfterWorkStarted, PlatformFeeRefundMode.RuleBased);
        p.AddRule(RefundCause.DisputeCustomerFavoured,           PlatformFeeRefundMode.Full);
        p.AddRule(RefundCause.DisputeProviderFavoured,           PlatformFeeRefundMode.None);
        p.AddRule(RefundCause.AdministrativeCorrection,          PlatformFeeRefundMode.Full);
    }
}

// ─── Update (header only) ────────────────────────────────────────────────────
public sealed class UpdateRefundAllocationPolicyCommand : AizenCommand<RefundAllocationPolicyMutateResultDto>
{
    public long      Id                   { get; init; }
    public decimal   NegativeBalanceLimit { get; init; }
    public DateTime  EffectiveFrom        { get; init; }
    public DateTime? EffectiveTo          { get; init; }
    public string?   Notes                { get; init; }
}

public sealed class UpdateRefundAllocationPolicyCommandHandler
    : AizenCommandHandler<UpdateRefundAllocationPolicyCommand, RefundAllocationPolicyMutateResultDto>
{
    private readonly IRefundAllocationPolicyRepository _policies;
    public UpdateRefundAllocationPolicyCommandHandler(IRefundAllocationPolicyRepository policies) => _policies = policies;

    public override async Task<RefundAllocationPolicyMutateResultDto?> Handle(UpdateRefundAllocationPolicyCommand r, CancellationToken ct)
    {
        var policy = await _policies.GetByIdAsync(r.Id, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.RefundAllocationPolicyInvalid, $"Refund-allocation policy {r.Id} not found.");
        policy.UpdateHeader(r.NegativeBalanceLimit, r.EffectiveFrom.ToUniversalTime(), r.EffectiveTo?.ToUniversalTime(), r.Notes);
        _policies.Update(policy);
        return new RefundAllocationPolicyMutateResultDto(policy.Id, policy.PolicyCode);
    }
}

// ─── Deactivate ──────────────────────────────────────────────────────────────
public sealed class DeactivateRefundAllocationPolicyCommand : AizenCommand<RefundAllocationPolicyMutateResultDto>
{
    public long Id { get; init; }
}

public sealed class DeactivateRefundAllocationPolicyCommandHandler
    : AizenCommandHandler<DeactivateRefundAllocationPolicyCommand, RefundAllocationPolicyMutateResultDto>
{
    private readonly IRefundAllocationPolicyRepository _policies;
    public DeactivateRefundAllocationPolicyCommandHandler(IRefundAllocationPolicyRepository policies) => _policies = policies;

    public override async Task<RefundAllocationPolicyMutateResultDto?> Handle(DeactivateRefundAllocationPolicyCommand r, CancellationToken ct)
    {
        var policy = await _policies.GetByIdAsync(r.Id, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.RefundAllocationPolicyInvalid, $"Refund-allocation policy {r.Id} not found.");
        policy.Deactivate();
        _policies.Update(policy);
        return new RefundAllocationPolicyMutateResultDto(policy.Id, policy.PolicyCode);
    }
}
