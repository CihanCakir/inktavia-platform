using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Command.CreateCommissionRule;

[DocumentationInfo("Create commission rule BFF command handler",
    "Forwards a new commission rule creation request to the Payment module. " +
    "RuleType and Priority are string values — the Payment module's JSON deserializer " +
    "converts them to enums. Returns the generated RuleCode and new entity Id.")]
public sealed class CreateCommissionRuleBffCommandHandler
    : AizenCommandHandler<CreateCommissionRuleBffCommand, CreateCommissionRuleBffCommandResponse>
{
    private readonly IPaymentRemoteCall _payment;

    public CreateCommissionRuleBffCommandHandler(IPaymentRemoteCall payment)
        => _payment = payment;

    public override async Task<CreateCommissionRuleBffCommandResponse?> Handle(
        CreateCommissionRuleBffCommand request, CancellationToken ct)
    {
        var result = await _payment.CreateCommissionRuleAsync(request.Body, ct);
        return new CreateCommissionRuleBffCommandResponse { Result = result };
    }
}
