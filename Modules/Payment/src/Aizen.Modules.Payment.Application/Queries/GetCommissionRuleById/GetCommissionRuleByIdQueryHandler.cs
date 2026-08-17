using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Application.Dto;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetCommissionRuleById;

[DocumentationInfo("GetCommissionRuleByIdQueryHandler",
    "Returns the full detail of a single commission rule by ID. " +
    "Throws PaymentErrorCode.CommissionRuleNotFound if the rule does not exist.")]
public sealed class GetCommissionRuleByIdQueryHandler
    : AizenQueryHandler<GetCommissionRuleByIdQuery, CommissionRuleDto>
{
    private readonly ICommissionRuleRepository _rules;

    public GetCommissionRuleByIdQueryHandler(ICommissionRuleRepository rules)
        => _rules = rules;

    public override async Task<CommissionRuleDto?> Handle(
        GetCommissionRuleByIdQuery request, CancellationToken ct)
    {
        var rule = await _rules.GetByIdAsync(request.Id, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.CommissionRuleNotFound);

        return new CommissionRuleDto(
            rule.Id, rule.RuleCode, rule.RuleType, rule.CategoryCode, rule.ProviderPlanId,
            rule.ProviderProfileId, rule.CommissionRate, rule.EffectiveFrom, rule.EffectiveTo,
            rule.Notes, rule.Status, rule.Priority, rule.ResolvedAppliedCount, rule.IsActive,
            rule.CreateUserId, rule.CreateDate, rule.ModifyUserId, rule.ModifyDate,
            rule.ContextType, rule.ProductCode, rule.SalesChannel,
            rule.RuleName, rule.CurrencyCode, rule.CommercialModel
        );
    }
}
