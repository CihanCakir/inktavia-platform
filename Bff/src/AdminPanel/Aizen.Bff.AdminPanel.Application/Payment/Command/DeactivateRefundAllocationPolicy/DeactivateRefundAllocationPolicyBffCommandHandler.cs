using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.DeactivateRefundAllocationPolicy;

[DocumentationInfo("Deactivate refund-allocation policy BFF command handler (P10)",
    "Forwards a deactivate (POST /admin/refund-allocation-policies/{id}/deactivate).")]
public sealed class DeactivateRefundAllocationPolicyBffCommandHandler
    : AizenCommandHandler<DeactivateRefundAllocationPolicyBffCommand, DeactivateRefundAllocationPolicyBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public DeactivateRefundAllocationPolicyBffCommandHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<DeactivateRefundAllocationPolicyBffResponse?> Handle(DeactivateRefundAllocationPolicyBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.DeactivateRefundAllocationPolicyAsync(request.Id, ct) };
}
