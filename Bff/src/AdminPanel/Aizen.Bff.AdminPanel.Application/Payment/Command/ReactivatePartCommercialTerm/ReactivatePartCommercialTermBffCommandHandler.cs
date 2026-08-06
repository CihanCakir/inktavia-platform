using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.ReactivatePartCommercialTerm;

[DocumentationInfo("Reactivate part commercial term BFF command handler (BE-S5)",
    "Forwards a reactivate (POST /part-commercial-term/rules/{id}/reactivate). The module re-runs the overlap guard, so " +
    "PartCommercialTermConflict / PartCommercialTermNotInactive may surface through the envelope. Admin-only.")]
public sealed class ReactivatePartCommercialTermBffCommandHandler
    : AizenCommandHandler<ReactivatePartCommercialTermBffCommand, ReactivatePartCommercialTermBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public ReactivatePartCommercialTermBffCommandHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<ReactivatePartCommercialTermBffResponse?> Handle(ReactivatePartCommercialTermBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.ReactivatePartCommercialTermAsync(request.Id, ct) };
}
