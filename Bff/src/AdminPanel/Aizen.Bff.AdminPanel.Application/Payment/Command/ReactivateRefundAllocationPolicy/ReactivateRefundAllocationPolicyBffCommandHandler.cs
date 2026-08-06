using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.ReactivateRefundAllocationPolicy;

[DocumentationInfo("Reactivate refund-allocation policy BFF command handler (P10)",
    "Forwards a reactivate (POST /admin/refund-allocation-policies/{id}/reactivate). A RefundAllocationPolicyConflict " +
    "surfaces through the envelope (fail-loud) when reactivation would overlap an active policy for the currency.")]
public sealed class ReactivateRefundAllocationPolicyBffCommandHandler
    : AizenCommandHandler<ReactivateRefundAllocationPolicyBffCommand, ReactivateRefundAllocationPolicyBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public ReactivateRefundAllocationPolicyBffCommandHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<ReactivateRefundAllocationPolicyBffResponse?> Handle(ReactivateRefundAllocationPolicyBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.ReactivateRefundAllocationPolicyAsync(request.Id, ct) };
}
