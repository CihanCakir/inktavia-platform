using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.CommissionBenefit;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Commands.CommissionBenefit;

public sealed class CreateProviderCommissionBenefitRuleCommand : AizenCommand<CreateProviderCommissionBenefitRuleResult>
{
    public string?                RuleName                   { get; init; }
    public long?                  ProviderProfileId          { get; init; }
    public long?                  ProviderPlanId             { get; init; }
    public List<string>?          ApplicableCategoryCodes    { get; init; }
    public required decimal       AdjustmentPercentagePoints { get; init; }
    public decimal                MinimumCommissionRate      { get; init; }
    public decimal?               MaximumDiscountAmount      { get; init; }
    public decimal?               MaximumEligibleGMV         { get; init; }
    public long?                  UsageLimit                 { get; init; }
    public bool                   Stackable                  { get; init; }
    public bool                   Exclusive                  { get; init; }
    public CommissionRulePriority Priority                   { get; init; } = CommissionRulePriority.Standard;
    public required DateTime      EffectiveFrom              { get; init; }
    public DateTime?              EffectiveTo                { get; init; }
    public string                 CurrencyCode               { get; init; } = "TRY";
    public string?                Notes                      { get; init; }
}

public sealed record CreateProviderCommissionBenefitRuleResult(long Id, string RuleCode);

[DocumentationInfo("CreateProviderCommissionBenefitRuleCommandHandler",
    "Admin creates a commission benefit rule. Validates coherence (no surcharge, not Exclusive+Stackable, min∈[0,1]) via " +
    "the domain factory and runs the overlap conflict guard before persisting.")]
public sealed class CreateProviderCommissionBenefitRuleCommandHandler
    : AizenCommandHandler<CreateProviderCommissionBenefitRuleCommand, CreateProviderCommissionBenefitRuleResult>
{
    private readonly IProviderCommissionBenefitRuleRepository _rules;
    public CreateProviderCommissionBenefitRuleCommandHandler(IProviderCommissionBenefitRuleRepository rules) => _rules = rules;

    public override async Task<CreateProviderCommissionBenefitRuleResult?> Handle(
        CreateProviderCommissionBenefitRuleCommand request, CancellationToken ct)
    {
        var ruleCode = await _rules.GenerateRuleCodeAsync(ct);

        var rule = ProviderCommissionBenefitRuleEntity.Create(
            ruleCode, request.RuleName, request.ProviderProfileId, request.ProviderPlanId,
            request.ApplicableCategoryCodes, request.AdjustmentPercentagePoints, request.MinimumCommissionRate,
            request.MaximumDiscountAmount, request.MaximumEligibleGMV, request.UsageLimit,
            request.Stackable, request.Exclusive, request.Priority,
            request.EffectiveFrom.ToUniversalTime(), request.EffectiveTo?.ToUniversalTime(),
            request.CurrencyCode, request.Notes);

        var conflict = await _rules.FindOverlappingActiveRuleAsync(rule, ct);
        if (conflict is not null)
            throw new AizenBusinessException(
                (int)PaymentErrorCode.ProviderCommissionBenefitRuleConflict,
                $"A conflicting active benefit rule already exists (Id={conflict.Id}, Code={conflict.RuleCode}).");

        await _rules.AddAsync(rule, ct);
        return new CreateProviderCommissionBenefitRuleResult(rule.Id, ruleCode);
    }
}
