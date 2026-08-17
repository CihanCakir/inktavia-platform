using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.ResolveRefundAllocationPolicy;

[DocumentationInfo("Resolve refund-allocation policy BFF query handler (P10)",
    "Point-in-time active refund-allocation policy resolution (GET /admin/refund-allocation-policies/resolve). Read-only.")]
public sealed class ResolveRefundAllocationPolicyBffQueryHandler
    : AizenQueryHandler<ResolveRefundAllocationPolicyBffQuery, ResolveRefundAllocationPolicyBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public ResolveRefundAllocationPolicyBffQueryHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<ResolveRefundAllocationPolicyBffResponse?> Handle(ResolveRefundAllocationPolicyBffQuery request, CancellationToken ct)
        => new() { Result = await _payment.ResolveRefundAllocationPolicyAsync(request.CurrencyCode, request.AtUtc, ct) };
}
