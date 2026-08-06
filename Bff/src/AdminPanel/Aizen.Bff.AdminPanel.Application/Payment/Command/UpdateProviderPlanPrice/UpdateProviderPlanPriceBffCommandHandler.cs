using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.UpdateProviderPlanPrice;

[DocumentationInfo("Update provider-plan-price BFF command handler (BE-P4)",
    "Forwards a plan-price update (PUT /plan-prices/{id}). Conflict/Gap surfaces through the envelope.")]
public sealed class UpdateProviderPlanPriceBffCommandHandler
    : AizenCommandHandler<UpdateProviderPlanPriceBffCommand, UpdateProviderPlanPriceBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public UpdateProviderPlanPriceBffCommandHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<UpdateProviderPlanPriceBffResponse?> Handle(UpdateProviderPlanPriceBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.UpdateProviderPlanPriceAsync(request.Id, request.Body with { Id = request.Id }, ct) };
}
