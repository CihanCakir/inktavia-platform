using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.CreatePremiumProductPrice;

[DocumentationInfo("Create premium product price BFF command handler (P11)",
    "Forwards a new versioned premium price (POST /admin/premium/prices).")]
public sealed class CreatePremiumProductPriceBffCommandHandler
    : AizenCommandHandler<CreatePremiumProductPriceBffCommand, CreatePremiumProductPriceBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public CreatePremiumProductPriceBffCommandHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<CreatePremiumProductPriceBffResponse?> Handle(CreatePremiumProductPriceBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.CreatePremiumProductPriceAsync(request.Body, ct) };
}
