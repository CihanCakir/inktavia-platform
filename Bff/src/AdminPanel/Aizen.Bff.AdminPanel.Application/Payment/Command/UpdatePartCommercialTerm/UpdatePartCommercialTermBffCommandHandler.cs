using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.UpdatePartCommercialTerm;

[DocumentationInfo("Update part commercial term BFF command handler (BE-S5)",
    "Forwards a part commercial term update (PUT /part-commercial-term/rules/{id}). Conflict/Invalid surfaces through the envelope. Admin-only.")]
public sealed class UpdatePartCommercialTermBffCommandHandler
    : AizenCommandHandler<UpdatePartCommercialTermBffCommand, UpdatePartCommercialTermBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public UpdatePartCommercialTermBffCommandHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<UpdatePartCommercialTermBffResponse?> Handle(UpdatePartCommercialTermBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.UpdatePartCommercialTermAsync(request.Id, request.Body with { Id = request.Id }, ct) };
}
