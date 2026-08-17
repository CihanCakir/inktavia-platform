using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Payment.Application.Dto;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetCustomerBenefitBudgetPoliciesList;

[DocumentationInfo("GetCustomerBenefitBudgetPoliciesListQueryHandler",
    "Returns the per-plan customer-benefit budget policy list (no paging — policies are few). Optional plan / currency / " +
    "active filters, applied in-memory. Sorted by plan then EffectiveFrom desc so the newest policy per plan reads first. " +
    "Used by the admin benefit-budget screen. Reserve/consume/release are runtime ops, not part of this admin surface.")]
public sealed class GetCustomerBenefitBudgetPoliciesListQueryHandler
    : AizenQueryHandler<GetCustomerBenefitBudgetPoliciesListQuery, CustomerBenefitBudgetPolicyListResult>
{
    private readonly ICustomerBenefitBudgetPolicyRepository _policies;

    public GetCustomerBenefitBudgetPoliciesListQueryHandler(ICustomerBenefitBudgetPolicyRepository policies)
        => _policies = policies;

    public override async Task<CustomerBenefitBudgetPolicyListResult?> Handle(
        GetCustomerBenefitBudgetPoliciesListQuery request, CancellationToken ct)
    {
        var all = await _policies.GetAllAsync(ct);

        IEnumerable<Domain.Entities.CustomerBenefit.CustomerBenefitBudgetPolicyEntity> filtered = all;

        if (request.CustomerPlanId.HasValue)
            filtered = filtered.Where(p => p.CustomerPlanId == request.CustomerPlanId.Value);
        if (!string.IsNullOrWhiteSpace(request.CurrencyCode))
        {
            var cur = request.CurrencyCode.ToUpperInvariant();
            filtered = filtered.Where(p => p.CurrencyCode == cur);
        }
        if (request.IsActive.HasValue)
            filtered = filtered.Where(p => p.IsActive == request.IsActive.Value);

        var items = filtered
            .OrderBy(p => p.CustomerPlanId)
            .ThenByDescending(p => p.EffectiveFrom)
            .Select(CustomerBenefitBudgetPolicyDtoMapper.ToDto)
            .ToList();

        return new CustomerBenefitBudgetPolicyListResult(items, items.Count);
    }
}
