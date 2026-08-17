using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.ResolveProfitProtectionPolicy;

[DocumentationInfo("Resolve profit-protection policy BFF query handler (BE-P5)",
    "Resolves the single active policy for a currency at an instant (GET /profit-protection/resolve). Read-only.")]
public sealed class ResolveProfitProtectionPolicyBffQueryHandler
    : AizenQueryHandler<ResolveProfitProtectionPolicyBffQuery, ResolveProfitProtectionPolicyBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public ResolveProfitProtectionPolicyBffQueryHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<ResolveProfitProtectionPolicyBffResponse?> Handle(ResolveProfitProtectionPolicyBffQuery request, CancellationToken ct)
        => new() { Result = await _payment.ResolveProfitProtectionPolicyAsync(request.CurrencyCode, request.AtUtc, ct) };
}
