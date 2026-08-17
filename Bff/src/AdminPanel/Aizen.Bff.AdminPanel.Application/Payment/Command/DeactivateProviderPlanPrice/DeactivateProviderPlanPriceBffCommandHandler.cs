using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.DeactivateProviderPlanPrice;

[DocumentationInfo("Deactivate provider-plan-price BFF command handler (BE-P4)",
    "Forwards a deactivate (POST /plan-prices/{id}/deactivate).")]
public sealed class DeactivateProviderPlanPriceBffCommandHandler
    : AizenCommandHandler<DeactivateProviderPlanPriceBffCommand, DeactivateProviderPlanPriceBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public DeactivateProviderPlanPriceBffCommandHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<DeactivateProviderPlanPriceBffResponse?> Handle(DeactivateProviderPlanPriceBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.DeactivateProviderPlanPriceAsync(request.Id, ct) };
}
