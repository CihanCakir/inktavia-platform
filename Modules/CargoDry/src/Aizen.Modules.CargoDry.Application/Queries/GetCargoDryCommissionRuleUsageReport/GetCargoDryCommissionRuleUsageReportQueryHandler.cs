using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryCommissionRuleUsageReport;

[DocumentationInfo("Get CargoDry commission rule usage report query handler",
    "Returns aggregated commission rule usage rows derived from CargoDrySalesAttributionEntity. " +
    "Grouped by ResolvedRuleId / RuleName / RuleSource / ProductCode / SalesChannel / ProviderProfileId. " +
    "Includes usage counts and total sale, provider-share, and platform-share amounts. " +
    "Phase 15 (July 2026).")]
public sealed class GetCargoDryCommissionRuleUsageReportQueryHandler
    : AizenQueryHandler<GetCargoDryCommissionRuleUsageReportQuery, GetCargoDryCommissionRuleUsageReportResponse>
{
    private readonly ICargoDrySalesAttributionRepository _attributions;

    public GetCargoDryCommissionRuleUsageReportQueryHandler(
        ICargoDrySalesAttributionRepository attributions)
        => _attributions = attributions;

    public override async Task<GetCargoDryCommissionRuleUsageReportResponse> Handle(
        GetCargoDryCommissionRuleUsageReportQuery request, CancellationToken ct)
    {
        var skip = (request.Page - 1) * request.PageSize;

        var (items, total) = await _attributions.GetCommissionRuleUsageGroupedAsync(
            request.DateFrom,
            request.DateTo,
            request.RuleId,
            request.ProductCode,
            request.SalesChannel,
            request.ProviderProfileId,
            skip,
            request.PageSize,
            ct);

        return new GetCargoDryCommissionRuleUsageReportResponse
        {
            Report = new CargoDryCommissionRuleUsageReportDto
            {
                Items    = items,
                Total    = total,
                Page     = request.Page,
                PageSize = request.PageSize,
            }
        };
    }
}
