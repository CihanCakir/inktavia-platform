using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Payment.Application.Dto;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetProviderCommissionBenefitEntitlementsList;

[DocumentationInfo("GetProviderCommissionBenefitEntitlementsListQueryHandler",
    "Returns the provider commission-benefit entitlement list (no paging). Optional provider / rule / active filters, " +
    "applied in-memory. Each row is enriched with the referenced benefit rule's code/name. Sorted by GrantedFrom desc " +
    "(newest grant first). Used by the admin entitlements screen (grant/revoke/list). Usage counters are read-only here.")]
public sealed class GetProviderCommissionBenefitEntitlementsListQueryHandler
    : AizenQueryHandler<GetProviderCommissionBenefitEntitlementsListQuery, ProviderCommissionBenefitEntitlementListResult>
{
    private readonly IProviderCommissionBenefitEntitlementRepository _entitlements;
    private readonly IProviderCommissionBenefitRuleRepository        _rules;

    public GetProviderCommissionBenefitEntitlementsListQueryHandler(
        IProviderCommissionBenefitEntitlementRepository entitlements,
        IProviderCommissionBenefitRuleRepository        rules)
    {
        _entitlements = entitlements;
        _rules        = rules;
    }

    public override async Task<ProviderCommissionBenefitEntitlementListResult?> Handle(
        GetProviderCommissionBenefitEntitlementsListQuery request, CancellationToken ct)
    {
        var all = await _entitlements.GetAllAsync(ct);

        IEnumerable<Domain.Entities.CommissionBenefit.ProviderCommissionBenefitEntitlementEntity> filtered = all;

        if (request.ProviderProfileId.HasValue)
            filtered = filtered.Where(e => e.ProviderProfileId == request.ProviderProfileId.Value);
        if (request.BenefitRuleId.HasValue)
            filtered = filtered.Where(e => e.BenefitRuleId == request.BenefitRuleId.Value);
        if (request.IsActive.HasValue)
            filtered = filtered.Where(e => e.IsActive == request.IsActive.Value);

        var list = filtered.OrderByDescending(e => e.GrantedFrom).ToList();

        // Enrich each row with its benefit rule's code/name (single fetch of the referenced rules).
        var ruleIds = list.Select(e => e.BenefitRuleId).Distinct().ToList();
        var rules   = new Dictionary<long, Domain.Entities.CommissionBenefit.ProviderCommissionBenefitRuleEntity>();
        foreach (var id in ruleIds)
        {
            var rule = await _rules.GetByIdAsync(id, ct);
            if (rule is not null) rules[id] = rule;
        }

        var items = list
            .Select(e => ProviderCommissionBenefitEntitlementDtoMapper.ToDto(
                e, rules.TryGetValue(e.BenefitRuleId, out var r) ? r : null))
            .ToList();

        return new ProviderCommissionBenefitEntitlementListResult(items, items.Count);
    }
}
