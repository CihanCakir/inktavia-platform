using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Payment.Application.Dto;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetCommissionRulesList;

[DocumentationInfo("GetCommissionRulesListQueryHandler",
    "Returns a paged list of commission rules with optional RuleType/Status/Priority filters. " +
    "Ordered by descending priority then descending EffectiveFrom. " +
    "Used by the admin commission rules list screen.")]
public sealed class GetCommissionRulesListQueryHandler
    : AizenQueryHandler<GetCommissionRulesListQuery, CommissionRuleListResult>
{
    private readonly ICommissionRuleRepository _rules;

    public GetCommissionRulesListQueryHandler(ICommissionRuleRepository rules)
        => _rules = rules;

    public override async Task<CommissionRuleListResult?> Handle(
        GetCommissionRulesListQuery request, CancellationToken ct)
    {
        var skip = (request.Page - 1) * request.PageSize;

        var (items, total) = await _rules.GetPagedAsync(
            request.RuleType, request.Status, request.Priority,
            skip, request.PageSize, ct);

        var dtos = items.Select(r => new CommissionRuleDto(
            r.Id, r.RuleCode, r.RuleType, r.CategoryCode, r.ProviderPlanId,
            r.ProviderProfileId, r.CommissionRate, r.EffectiveFrom, r.EffectiveTo,
            r.Notes, r.Status, r.Priority, r.ResolvedAppliedCount, r.IsActive,
            r.CreateUserId, r.CreateDate, r.ModifyUserId, r.ModifyDate,
            r.ContextType, r.ProductCode, r.SalesChannel
        )).ToList();

        return new CommissionRuleListResult(dtos, total, request.Page, request.PageSize);
    }
}
