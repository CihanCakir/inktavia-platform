using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.UpdatePremiumProductPrice;

[DocumentationInfo("Update premium product price BFF command handler (P11)",
    "Forwards a premium price update (PUT /admin/premium/prices/{id}). Id is applied from the route by the module.")]
public sealed class UpdatePremiumProductPriceBffCommandHandler
    : AizenCommandHandler<UpdatePremiumProductPriceBffCommand, UpdatePremiumProductPriceBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public UpdatePremiumProductPriceBffCommandHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<UpdatePremiumProductPriceBffResponse?> Handle(UpdatePremiumProductPriceBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.UpdatePremiumProductPriceAsync(request.Id, request.Body, ct) };
}
