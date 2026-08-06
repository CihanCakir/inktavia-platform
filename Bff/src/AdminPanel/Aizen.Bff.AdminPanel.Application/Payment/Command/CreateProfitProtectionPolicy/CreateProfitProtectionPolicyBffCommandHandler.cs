using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.CreateProfitProtectionPolicy;

[DocumentationInfo("Create profit-protection policy BFF command handler (BE-P5)",
    "Forwards a new profit-protection policy (POST /profit-protection/policies). ProfitProtectionPolicyConflict/Invalid surfaces through the envelope.")]
public sealed class CreateProfitProtectionPolicyBffCommandHandler
    : AizenCommandHandler<CreateProfitProtectionPolicyBffCommand, CreateProfitProtectionPolicyBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public CreateProfitProtectionPolicyBffCommandHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<CreateProfitProtectionPolicyBffResponse?> Handle(CreateProfitProtectionPolicyBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.CreateProfitProtectionPolicyAsync(request.Body, ct) };
}
