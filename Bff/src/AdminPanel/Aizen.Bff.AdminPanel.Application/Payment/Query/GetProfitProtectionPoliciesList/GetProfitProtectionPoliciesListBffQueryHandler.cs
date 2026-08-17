using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetProfitProtectionPoliciesList;

[DocumentationInfo("Get profit-protection policies list BFF query handler (BE-P5)",
    "Returns the profit-protection policy version history (no paging — one active per currency plus historical " +
    "versions) with optional currency / status / active filters, forwarded as plain strings to the Payment module. " +
    "Sorted by currency then EffectiveFrom desc. Read-only.")]
public sealed class GetProfitProtectionPoliciesListBffQueryHandler
    : AizenQueryHandler<GetProfitProtectionPoliciesListBffQuery, GetProfitProtectionPoliciesListBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public GetProfitProtectionPoliciesListBffQueryHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<GetProfitProtectionPoliciesListBffResponse?> Handle(GetProfitProtectionPoliciesListBffQuery request, CancellationToken ct)
        => new() { Result = await _payment.ListProfitProtectionPoliciesAsync(
            request.CurrencyCode, request.Status, request.IsActive, ct) };
}
