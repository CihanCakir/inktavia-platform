using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.ReactivatePlatformFeeRule;

[DocumentationInfo("Reactivate platform-fee rule BFF command handler (BE-P3)",
    "Forwards a reactivate (POST /platform-fee/rules/{id}/reactivate). A PlatformFeeRuleConflict/NotInactive surfaces " +
    "through the envelope (fail-loud) when reactivation would collide with an active rule.")]
public sealed class ReactivatePlatformFeeRuleBffCommandHandler
    : AizenCommandHandler<ReactivatePlatformFeeRuleBffCommand, ReactivatePlatformFeeRuleBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public ReactivatePlatformFeeRuleBffCommandHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<ReactivatePlatformFeeRuleBffResponse?> Handle(ReactivatePlatformFeeRuleBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.ReactivatePlatformFeeRuleAsync(request.Id, ct) };
}
