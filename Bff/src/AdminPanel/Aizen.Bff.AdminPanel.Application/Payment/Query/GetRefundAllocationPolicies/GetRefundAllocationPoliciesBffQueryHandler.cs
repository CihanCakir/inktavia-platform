using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetRefundAllocationPolicies;

[DocumentationInfo("Get refund-allocation policies BFF query handler (P10)",
    "Lists all refund-allocation policies (GET /admin/refund-allocation-policies). Read-only.")]
public sealed class GetRefundAllocationPoliciesBffQueryHandler
    : AizenQueryHandler<GetRefundAllocationPoliciesBffQuery, GetRefundAllocationPoliciesBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public GetRefundAllocationPoliciesBffQueryHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<GetRefundAllocationPoliciesBffResponse?> Handle(GetRefundAllocationPoliciesBffQuery request, CancellationToken ct)
        => new() { Items = await _payment.GetRefundAllocationPoliciesAsync(ct) };
}
