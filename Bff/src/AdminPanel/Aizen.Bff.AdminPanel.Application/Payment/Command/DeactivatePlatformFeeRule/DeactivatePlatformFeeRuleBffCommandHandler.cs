using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.DeactivatePlatformFeeRule;

[DocumentationInfo("Deactivate platform-fee rule BFF command handler (BE-P3)",
    "Forwards a deactivate (POST /platform-fee/rules/{id}/deactivate).")]
public sealed class DeactivatePlatformFeeRuleBffCommandHandler
    : AizenCommandHandler<DeactivatePlatformFeeRuleBffCommand, DeactivatePlatformFeeRuleBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public DeactivatePlatformFeeRuleBffCommandHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<DeactivatePlatformFeeRuleBffResponse?> Handle(DeactivatePlatformFeeRuleBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.DeactivatePlatformFeeRuleAsync(request.Id, ct) };
}
