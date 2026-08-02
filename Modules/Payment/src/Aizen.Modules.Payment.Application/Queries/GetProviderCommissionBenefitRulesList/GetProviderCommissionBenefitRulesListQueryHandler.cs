using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Payment.Application.Dto;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetProviderCommissionBenefitRulesList;

[DocumentationInfo("GetProviderCommissionBenefitRulesListQueryHandler",
    "Returns the provider commission-benefit rule list (no paging — rules are few). Optional provider / plan / category / " +
    "currency / stackable / active filters, applied in-memory. Sorted by currency then EffectiveFrom desc so the newest " +
    "rule per currency reads first. Used by the admin provider commission-benefit rules screen.")]
public sealed class GetProviderCommissionBenefitRulesListQueryHandler
    : AizenQueryHandler<GetProviderCommissionBenefitRulesListQuery, ProviderCommissionBenefitRuleListResult>
{
    private readonly IProviderCommissionBenefitRuleRepository _rules;

    public GetProviderCommissionBenefitRulesListQueryHandler(IProviderCommissionBenefitRuleRepository rules)
        => _rules = rules;

    public override async Task<ProviderCommissionBenefitRuleListResult?> Handle(
        GetProviderCommissionBenefitRulesListQuery request, CancellationToken ct)
    {
        var all = await _rules.GetAllAsync(ct);

        IEnumerable<Domain.Entities.CommissionBenefit.ProviderCommissionBenefitRuleEntity> filtered = all;

        if (request.ProviderProfileId.HasValue)
            filtered = filtered.Where(r => r.ProviderProfileId == request.ProviderProfileId.Value);
        if (request.ProviderPlanId.HasValue)
            filtered = filtered.Where(r => r.ProviderPlanId == request.ProviderPlanId.Value);
        if (!string.IsNullOrWhiteSpace(request.CategoryCode))
        {
            var cat = request.CategoryCode.ToUpperInvariant();
            filtered = filtered.Where(r => r.ApplicableCategoryCodes.Contains(cat));
        }
        if (!string.IsNullOrWhiteSpace(request.CurrencyCode))
        {
            var cur = request.CurrencyCode.ToUpperInvariant();
            filtered = filtered.Where(r => r.CurrencyCode == cur);
        }
        if (request.Stackable.HasValue)
            filtered = filtered.Where(r => r.Stackable == request.Stackable.Value);
        if (request.IsActive.HasValue)
            filtered = filtered.Where(r => r.IsActive == request.IsActive.Value);

        var items = filtered
            .OrderBy(r => r.CurrencyCode)
            .ThenByDescending(r => r.EffectiveFrom)
            .Select(ProviderCommissionBenefitRuleDtoMapper.ToDto)
            .ToList();

        return new ProviderCommissionBenefitRuleListResult(items, items.Count);
    }
}
