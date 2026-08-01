using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Payment.Application.Dto;
using Aizen.Modules.Payment.Domain.Entities.PlatformFee;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetPlatformFeeRulesList;

[DocumentationInfo("GetPlatformFeeRulesListQueryHandler",
    "Returns a paged list of platform fee rules with optional Model/Status/Currency/Category/CustomerType filters. " +
    "Ordered by descending priority then descending EffectiveFrom (repository order). " +
    "SpecificityRank is computed by the domain resolver. Used by the admin platform fee rules list screen.")]
public sealed class GetPlatformFeeRulesListQueryHandler
    : AizenQueryHandler<GetPlatformFeeRulesListQuery, PlatformFeeRuleListResult>
{
    private readonly IPlatformFeeRuleRepository _rules;

    public GetPlatformFeeRulesListQueryHandler(IPlatformFeeRuleRepository rules)
        => _rules = rules;

    public override async Task<PlatformFeeRuleListResult?> Handle(
        GetPlatformFeeRulesListQuery request, CancellationToken ct)
    {
        var skip = (request.Page - 1) * request.PageSize;

        var (items, total) = await _rules.GetPagedAsync(
            model:        request.Model,
            status:       request.Status,
            currencyCode: request.CurrencyCode,
            categoryCode: request.CategoryCode,
            customerType: request.CustomerType,
            skip:         skip,
            take:         request.PageSize,
            ct:           ct);

        var dtos = items.Select(PlatformFeeRuleDtoMapper.ToDto).ToList();

        return new PlatformFeeRuleListResult(dtos, total, request.Page, request.PageSize);
    }
}
