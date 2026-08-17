using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.UpdatePremiumProduct;

[DocumentationInfo("Update premium product BFF command handler (P11)",
    "Forwards a premium product update (PUT /admin/premium/products/{id}). Id is applied from the route by the module.")]
public sealed class UpdatePremiumProductBffCommandHandler
    : AizenCommandHandler<UpdatePremiumProductBffCommand, UpdatePremiumProductBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public UpdatePremiumProductBffCommandHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<UpdatePremiumProductBffResponse?> Handle(UpdatePremiumProductBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.UpdatePremiumProductAsync(request.Id, request.Body, ct) };
}
