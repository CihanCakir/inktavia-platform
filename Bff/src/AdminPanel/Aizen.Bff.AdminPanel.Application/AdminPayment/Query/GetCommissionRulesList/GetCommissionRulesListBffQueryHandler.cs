using Aizen.Bff.AdminPanel.Application.AdminPayment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Query.GetCommissionRulesList;

[DocumentationInfo("Get commission rules list BFF query handler",
    "Returns a paged list of commission rules with optional filters forwarded as plain strings " +
    "to the Payment module (enum parsing handled by the module's own converter). " +
    "Phase 13 (July 2026): Extended with ContextType, CommercialModel, ProductCode, SalesChannel, " +
    "Search, ProviderProfileId, and EffectiveOnUtc filters. " +
    "No identity enrichment needed — rules are admin-managed records, not user-linked.")]
public sealed class GetCommissionRulesListBffQueryHandler
    : AizenQueryHandler<GetCommissionRulesListBffQuery, GetCommissionRulesListBffResponse>
{
    private readonly IPaymentRemoteCall _payment;

    public GetCommissionRulesListBffQueryHandler(IPaymentRemoteCall payment)
        => _payment = payment;

    public override async Task<GetCommissionRulesListBffResponse?> Handle(
        GetCommissionRulesListBffQuery request, CancellationToken ct)
    {
        var result = await _payment.GetCommissionRulesPagedAsync(
            ruleType:          request.RuleType,
            status:            request.Status,
            priority:          request.Priority,
            page:              request.Page,
            pageSize:          request.PageSize,
            contextType:       request.ContextType,
            commercialModel:   request.CommercialModel,
            productCode:       request.ProductCode,
            salesChannel:      request.SalesChannel,
            search:            request.Search,
            providerProfileId: request.ProviderProfileId,
            effectiveOnUtc:    request.EffectiveOnUtc,
            ct:                ct);

        return new GetCommissionRulesListBffResponse { Result = result };
    }
}
