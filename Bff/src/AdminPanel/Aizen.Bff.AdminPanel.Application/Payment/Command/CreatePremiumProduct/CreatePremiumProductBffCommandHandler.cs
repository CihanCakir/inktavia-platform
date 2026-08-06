using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.CreatePremiumProduct;

[DocumentationInfo("Create premium product BFF command handler (P11)",
    "Forwards a new premium product to the Payment module (POST /admin/premium/products). EntitlementType is a string enum name.")]
public sealed class CreatePremiumProductBffCommandHandler
    : AizenCommandHandler<CreatePremiumProductBffCommand, CreatePremiumProductBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public CreatePremiumProductBffCommandHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<CreatePremiumProductBffResponse?> Handle(CreatePremiumProductBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.CreatePremiumProductAsync(request.Body, ct) };
}
