using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.ReactivateProfitProtectionPolicy;

[DocumentationInfo("Reactivate profit-protection policy BFF command handler (BE-P5)",
    "Forwards a reactivate (POST /profit-protection/policies/{id}/reactivate). The module re-runs the single-active " +
    "overlap guard for the currency, so ProfitProtectionPolicyConflict may surface through the envelope.")]
public sealed class ReactivateProfitProtectionPolicyBffCommandHandler
    : AizenCommandHandler<ReactivateProfitProtectionPolicyBffCommand, ReactivateProfitProtectionPolicyBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public ReactivateProfitProtectionPolicyBffCommandHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<ReactivateProfitProtectionPolicyBffResponse?> Handle(ReactivateProfitProtectionPolicyBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.ReactivateProfitProtectionPolicyAsync(request.Id, ct) };
}
