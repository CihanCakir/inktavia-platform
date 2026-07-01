using Aizen.Bff.AdminPanel.Application.AdminPayment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Query.GetCommissionRulesList;

[DocumentationInfo("Get commission rules list BFF query handler",
    "Returns a paged list of commission rules with optional RuleType/Status/Priority string filters. " +
    "Filters are forwarded as plain strings to the Payment module; the Payment module " +
    "parses enum values from the query string via its own enum converter. " +
    "No identity enrichment needed — rules are admin-managed records, not user-linked.")]
public sealed class GetCommissionRulesListBffQueryHandler
    : AizenQueryHandler<GetCommissionRulesListBffQuery, GetCommissionRulesListBffResponse>
{
    private readonly IAdminPaymentBffRemoteCall _payment;

    public GetCommissionRulesListBffQueryHandler(IAdminPaymentBffRemoteCall payment)
        => _payment = payment;

    public override async Task<GetCommissionRulesListBffResponse?> Handle(
        GetCommissionRulesListBffQuery request, CancellationToken ct)
    {
        var result = await _payment.GetCommissionRulesPagedAsync(
            request.RuleType,
            request.Status,
            request.Priority,
            request.Page,
            request.PageSize,
            ct);

        return new GetCommissionRulesListBffResponse { Result = result };
    }
}
