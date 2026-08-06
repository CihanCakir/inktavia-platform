using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.DeactivatePartCommercialTerm;

[DocumentationInfo("Deactivate part commercial term BFF command handler (BE-S5)",
    "Forwards a deactivate (POST /part-commercial-term/rules/{id}/deactivate). Admin-only.")]
public sealed class DeactivatePartCommercialTermBffCommandHandler
    : AizenCommandHandler<DeactivatePartCommercialTermBffCommand, DeactivatePartCommercialTermBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public DeactivatePartCommercialTermBffCommandHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<DeactivatePartCommercialTermBffResponse?> Handle(DeactivatePartCommercialTermBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.DeactivatePartCommercialTermAsync(request.Id, ct) };
}
