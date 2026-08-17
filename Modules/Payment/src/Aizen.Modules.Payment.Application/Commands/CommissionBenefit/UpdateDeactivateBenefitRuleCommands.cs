using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Commands.CommissionBenefit;

// ── Update ───────────────────────────────────────────────────────────────────

public sealed class UpdateProviderCommissionBenefitRuleCommand : AizenCommand<BenefitRuleMutationResult>
{
    public required long          Id                         { get; init; }
    public string?                RuleName                   { get; init; }
    public List<string>?          ApplicableCategoryCodes    { get; init; }
    public required decimal       AdjustmentPercentagePoints { get; init; }
    public decimal                MinimumCommissionRate      { get; init; }
    public decimal?               MaximumDiscountAmount      { get; init; }
    public decimal?               MaximumEligibleGMV         { get; init; }
    public long?                  UsageLimit                 { get; init; }
    public bool                   Stackable                  { get; init; }
    public bool                   Exclusive                  { get; init; }
    public CommissionRulePriority Priority                   { get; init; }
    public required DateTime      EffectiveFrom              { get; init; }
    public DateTime?              EffectiveTo                { get; init; }
    public string?                Notes                      { get; init; }
}

[DocumentationInfo("UpdateProviderCommissionBenefitRuleCommandHandler",
    "Admin updates a benefit rule. Re-validates coherence and runs the overlap conflict guard (self excluded).")]
public sealed class UpdateProviderCommissionBenefitRuleCommandHandler
    : AizenCommandHandler<UpdateProviderCommissionBenefitRuleCommand, BenefitRuleMutationResult>
{
    private readonly IProviderCommissionBenefitRuleRepository _rules;
    public UpdateProviderCommissionBenefitRuleCommandHandler(IProviderCommissionBenefitRuleRepository rules) => _rules = rules;

    public override async Task<BenefitRuleMutationResult?> Handle(
        UpdateProviderCommissionBenefitRuleCommand request, CancellationToken ct)
    {
        var rule = await _rules.GetByIdAsync(request.Id, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.ProviderCommissionBenefitRuleNotFound);

        rule.Update(request.RuleName, request.ApplicableCategoryCodes, request.AdjustmentPercentagePoints,
            request.MinimumCommissionRate, request.MaximumDiscountAmount, request.MaximumEligibleGMV, request.UsageLimit,
            request.Stackable, request.Exclusive, request.Priority,
            request.EffectiveFrom.ToUniversalTime(), request.EffectiveTo?.ToUniversalTime(), request.Notes);

        var conflict = await _rules.FindOverlappingActiveRuleAsync(rule, ct);
        if (conflict is not null)
            throw new AizenBusinessException(
                (int)PaymentErrorCode.ProviderCommissionBenefitRuleConflict,
                $"The update would conflict with an existing active rule (Id={conflict.Id}, Code={conflict.RuleCode}).");

        _rules.Update(rule);
        return new BenefitRuleMutationResult(rule.Id, rule.RuleCode);
    }
}

// ── Deactivate ───────────────────────────────────────────────────────────────

public sealed class DeactivateProviderCommissionBenefitRuleCommand : AizenCommand<BenefitRuleMutationResult>
{
    public required long Id { get; init; }
}

[DocumentationInfo("DeactivateProviderCommissionBenefitRuleCommandHandler",
    "Admin deactivates a benefit rule (Status=Inactive). Throws ProviderCommissionBenefitRuleNotFound if missing.")]
public sealed class DeactivateProviderCommissionBenefitRuleCommandHandler
    : AizenCommandHandler<DeactivateProviderCommissionBenefitRuleCommand, BenefitRuleMutationResult>
{
    private readonly IProviderCommissionBenefitRuleRepository _rules;
    public DeactivateProviderCommissionBenefitRuleCommandHandler(IProviderCommissionBenefitRuleRepository rules) => _rules = rules;

    public override async Task<BenefitRuleMutationResult?> Handle(
        DeactivateProviderCommissionBenefitRuleCommand request, CancellationToken ct)
    {
        var rule = await _rules.GetByIdAsync(request.Id, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.ProviderCommissionBenefitRuleNotFound);
        rule.Deactivate();
        _rules.Update(rule);
        return new BenefitRuleMutationResult(rule.Id, rule.RuleCode);
    }
}

public sealed record BenefitRuleMutationResult(long Id, string? RuleCode);
