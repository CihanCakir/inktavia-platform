using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.CreatePlatformFeeRule;

[DocumentationInfo("Create platform-fee rule BFF command handler (BE-P3)",
    "Forwards a new platform-fee rule to the Payment module (POST /platform-fee/rules). Model/Priority are string enum " +
    "names parsed by the module. A PlatformFeeRuleConflict/Invalid surfaces through the envelope (fail-loud).")]
public sealed class CreatePlatformFeeRuleBffCommandHandler
    : AizenCommandHandler<CreatePlatformFeeRuleBffCommand, CreatePlatformFeeRuleBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public CreatePlatformFeeRuleBffCommandHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<CreatePlatformFeeRuleBffResponse?> Handle(CreatePlatformFeeRuleBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.CreatePlatformFeeRuleAsync(request.Body, ct) };
}
