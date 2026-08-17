using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Payment.Application.Dto;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetCustomerDiscountRulesList;

[DocumentationInfo("GetCustomerDiscountRulesListQueryHandler",
    "Returns the customer-discount rule list (no paging — rules are few). Optional plan / category / currency / funding / " +
    "active filters, applied in-memory. Sorted by currency then EffectiveFrom desc so the newest rule per currency reads " +
    "first. Used by the admin customer-discount rules screen.")]
public sealed class GetCustomerDiscountRulesListQueryHandler
    : AizenQueryHandler<GetCustomerDiscountRulesListQuery, CustomerDiscountRuleListResult>
{
    private readonly ICustomerDiscountRuleRepository _rules;

    public GetCustomerDiscountRulesListQueryHandler(ICustomerDiscountRuleRepository rules)
        => _rules = rules;

    public override async Task<CustomerDiscountRuleListResult?> Handle(
        GetCustomerDiscountRulesListQuery request, CancellationToken ct)
    {
        var all = await _rules.GetAllAsync(ct);

        IEnumerable<Domain.Entities.CustomerDiscount.CustomerDiscountRuleEntity> filtered = all;

        if (request.CustomerPlanId.HasValue)
            filtered = filtered.Where(r => r.CustomerPlanId == request.CustomerPlanId.Value);
        if (!string.IsNullOrWhiteSpace(request.CategoryCode))
        {
            var cat = request.CategoryCode.ToUpperInvariant();
            filtered = filtered.Where(r => r.CategoryCode == cat);
        }
        if (!string.IsNullOrWhiteSpace(request.CurrencyCode))
        {
            var cur = request.CurrencyCode.ToUpperInvariant();
            filtered = filtered.Where(r => r.CurrencyCode == cur);
        }
        if (request.FundingMode.HasValue)
            filtered = filtered.Where(r => r.FundingMode == request.FundingMode.Value);
        if (request.IsActive.HasValue)
            filtered = filtered.Where(r => r.IsActive == request.IsActive.Value);

        var items = filtered
            .OrderBy(r => r.CurrencyCode)
            .ThenByDescending(r => r.EffectiveFrom)
            .Select(CustomerDiscountRuleDtoMapper.ToDto)
            .ToList();

        return new CustomerDiscountRuleListResult(items, items.Count);
    }
}
