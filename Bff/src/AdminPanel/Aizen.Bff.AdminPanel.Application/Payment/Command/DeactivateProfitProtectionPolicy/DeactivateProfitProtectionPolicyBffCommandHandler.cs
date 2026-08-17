using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.DeactivateProfitProtectionPolicy;

[DocumentationInfo("Deactivate profit-protection policy BFF command handler (BE-P5)",
    "Forwards a deactivate (POST /profit-protection/policies/{id}/deactivate).")]
public sealed class DeactivateProfitProtectionPolicyBffCommandHandler
    : AizenCommandHandler<DeactivateProfitProtectionPolicyBffCommand, DeactivateProfitProtectionPolicyBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public DeactivateProfitProtectionPolicyBffCommandHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<DeactivateProfitProtectionPolicyBffResponse?> Handle(DeactivateProfitProtectionPolicyBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.DeactivateProfitProtectionPolicyAsync(request.Id, ct) };
}
