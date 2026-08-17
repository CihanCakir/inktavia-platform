using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.ActivatePremiumProduct;

[DocumentationInfo("Activate premium product BFF command handler (P11)",
    "Forwards an activate (POST /admin/premium/products/{id}/activate).")]
public sealed class ActivatePremiumProductBffCommandHandler
    : AizenCommandHandler<ActivatePremiumProductBffCommand, ActivatePremiumProductBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public ActivatePremiumProductBffCommandHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<ActivatePremiumProductBffResponse?> Handle(ActivatePremiumProductBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.ActivatePremiumProductAsync(request.Id, ct) };
}
