using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetRefundAllocationPolicyById;

[DocumentationInfo("Get refund-allocation policy by id BFF query handler (P10)",
    "Reads a single refund-allocation policy (GET /admin/refund-allocation-policies/{id}). Read-only.")]
public sealed class GetRefundAllocationPolicyByIdBffQueryHandler
    : AizenQueryHandler<GetRefundAllocationPolicyByIdBffQuery, GetRefundAllocationPolicyByIdBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public GetRefundAllocationPolicyByIdBffQueryHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<GetRefundAllocationPolicyByIdBffResponse?> Handle(GetRefundAllocationPolicyByIdBffQuery request, CancellationToken ct)
        => new() { Result = await _payment.GetRefundAllocationPolicyByIdAsync(request.Id, ct) };
}
