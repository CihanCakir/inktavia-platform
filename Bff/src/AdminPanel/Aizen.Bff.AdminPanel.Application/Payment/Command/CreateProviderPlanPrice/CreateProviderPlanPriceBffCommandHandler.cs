using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.CreateProviderPlanPrice;

[DocumentationInfo("Create provider-plan-price BFF command handler (BE-P4)",
    "Forwards a new versioned plan price (POST /plan-prices). ProviderPlanPriceConflict/Gap surfaces through the envelope.")]
public sealed class CreateProviderPlanPriceBffCommandHandler
    : AizenCommandHandler<CreateProviderPlanPriceBffCommand, CreateProviderPlanPriceBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public CreateProviderPlanPriceBffCommandHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<CreateProviderPlanPriceBffResponse?> Handle(CreateProviderPlanPriceBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.CreateProviderPlanPriceAsync(request.Body, ct) };
}
