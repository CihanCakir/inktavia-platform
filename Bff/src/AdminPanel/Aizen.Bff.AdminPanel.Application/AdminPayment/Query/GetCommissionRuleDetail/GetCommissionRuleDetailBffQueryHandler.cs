using Aizen.Bff.AdminPanel.Application.AdminPayment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Query.GetCommissionRuleDetail;

[DocumentationInfo("Get commission rule detail BFF query handler",
    "Fetches a single commission rule by ID from the Payment module. " +
    "Returns null body (404 passthrough) when the Payment module throws CommissionRuleNotFound. " +
    "No cross-module enrichment required at MVP; ProviderDisplayName and PlanName " +
    "are left null until Identity/Plan enrichment is wired in a later iteration.")]
public sealed class GetCommissionRuleDetailBffQueryHandler
    : AizenQueryHandler<GetCommissionRuleDetailBffQuery, GetCommissionRuleDetailBffResponse>
{
    private readonly IAdminPaymentBffRemoteCall _payment;

    public GetCommissionRuleDetailBffQueryHandler(IAdminPaymentBffRemoteCall payment)
        => _payment = payment;

    public override async Task<GetCommissionRuleDetailBffResponse?> Handle(
        GetCommissionRuleDetailBffQuery request, CancellationToken ct)
    {
        var rule = await _payment.GetCommissionRuleByIdAsync(request.Id, ct);
        return new GetCommissionRuleDetailBffResponse { Rule = rule };
    }
}
