using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.CreateRefundAllocationPolicy;

[DocumentationInfo("Create refund-allocation policy BFF command handler (P10)",
    "Forwards a new refund-allocation policy (POST /admin/refund-allocation-policies). Rule Cause/Mode are string enum names.")]
public sealed class CreateRefundAllocationPolicyBffCommandHandler
    : AizenCommandHandler<CreateRefundAllocationPolicyBffCommand, CreateRefundAllocationPolicyBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public CreateRefundAllocationPolicyBffCommandHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<CreateRefundAllocationPolicyBffResponse?> Handle(CreateRefundAllocationPolicyBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.CreateRefundAllocationPolicyAsync(request.Body, ct) };
}
