using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.DeactivatePremiumProduct;

[DocumentationInfo("Deactivate premium product BFF command handler (P11)",
    "Forwards a deactivate (POST /admin/premium/products/{id}/deactivate).")]
public sealed class DeactivatePremiumProductBffCommandHandler
    : AizenCommandHandler<DeactivatePremiumProductBffCommand, DeactivatePremiumProductBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public DeactivatePremiumProductBffCommandHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<DeactivatePremiumProductBffResponse?> Handle(DeactivatePremiumProductBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.DeactivatePremiumProductAsync(request.Id, ct) };
}
