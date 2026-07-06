using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Payment.Application.Dto;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetCommissionRulesList;

[DocumentationInfo("GetCommissionRulesListQueryHandler",
    "Returns a paged list of commission rules with optional RuleType/Status/Priority filters. " +
    "Phase 13 (July 2026): Extended with ContextType, CommercialModel, ProductCode, SalesChannel, " +
    "Search, ProviderProfileId, and EffectiveOnUtc filters. " +
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
            ruleType:          request.RuleType,
            status:            request.Status,
            priority:          request.Priority,
            skip:              skip,
            take:              request.PageSize,
            contextType:       request.ContextType,
            commercialModel:   request.CommercialModel,
            productCode:       request.ProductCode,
            salesChannel:      request.SalesChannel,
            search:            request.Search,
            providerProfileId: request.ProviderProfileId,
            effectiveOnUtc:    request.EffectiveOnUtc,
            ct:                ct);

        var dtos = items.Select(r => new CommissionRuleDto(
            r.Id, r.RuleCode, r.RuleType, r.CategoryCode, r.ProviderPlanId,
            r.ProviderProfileId, r.CommissionRate, r.EffectiveFrom, r.EffectiveTo,
            r.Notes, r.Status, r.Priority, r.ResolvedAppliedCount, r.IsActive,
            r.CreateUserId, r.CreateDate, r.ModifyUserId, r.ModifyDate,
            r.ContextType, r.ProductCode, r.SalesChannel,
            r.RuleName, r.CurrencyCode, r.CommercialModel
        )).ToList();

        return new CommissionRuleListResult(dtos, total, request.Page, request.PageSize);
    }
}
