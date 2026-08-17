using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.DeactivatePremiumProductPrice;

[DocumentationInfo("Deactivate premium product price BFF command handler (P11)",
    "Forwards a deactivate (POST /admin/premium/prices/{id}/deactivate).")]
public sealed class DeactivatePremiumProductPriceBffCommandHandler
    : AizenCommandHandler<DeactivatePremiumProductPriceBffCommand, DeactivatePremiumProductPriceBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public DeactivatePremiumProductPriceBffCommandHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<DeactivatePremiumProductPriceBffResponse?> Handle(DeactivatePremiumProductPriceBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.DeactivatePremiumProductPriceAsync(request.Id, ct) };
}
