using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.UpdateProfitProtectionPolicy;

[DocumentationInfo("Update profit-protection policy BFF command handler (BE-P5)",
    "Forwards a policy update (PUT /profit-protection/policies/{id}). Conflict/Invalid surfaces through the envelope.")]
public sealed class UpdateProfitProtectionPolicyBffCommandHandler
    : AizenCommandHandler<UpdateProfitProtectionPolicyBffCommand, UpdateProfitProtectionPolicyBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public UpdateProfitProtectionPolicyBffCommandHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<UpdateProfitProtectionPolicyBffResponse?> Handle(UpdateProfitProtectionPolicyBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.UpdateProfitProtectionPolicyAsync(request.Id, request.Body with { Id = request.Id }, ct) };
}
