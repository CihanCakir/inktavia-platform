using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.UpdateRefundAllocationPolicy;

[DocumentationInfo("Update refund-allocation policy BFF command handler (P10)",
    "Forwards a refund-allocation policy update (PUT /admin/refund-allocation-policies/{id}). Id is applied from the route by the module.")]
public sealed class UpdateRefundAllocationPolicyBffCommandHandler
    : AizenCommandHandler<UpdateRefundAllocationPolicyBffCommand, UpdateRefundAllocationPolicyBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public UpdateRefundAllocationPolicyBffCommandHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<UpdateRefundAllocationPolicyBffResponse?> Handle(UpdateRefundAllocationPolicyBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.UpdateRefundAllocationPolicyAsync(request.Id, request.Body, ct) };
}
