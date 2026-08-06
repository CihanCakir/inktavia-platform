using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.CreatePartCommercialTerm;

[DocumentationInfo("Create part commercial term BFF command handler (BE-S5)",
    "Forwards a new part commercial term (POST /part-commercial-term/rules). Versioning is a side effect — a new version " +
    "row is appended for the scope. PartCommercialTermConflict/Invalid surfaces through the envelope. Admin-only.")]
public sealed class CreatePartCommercialTermBffCommandHandler
    : AizenCommandHandler<CreatePartCommercialTermBffCommand, CreatePartCommercialTermBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public CreatePartCommercialTermBffCommandHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<CreatePartCommercialTermBffResponse?> Handle(CreatePartCommercialTermBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.CreatePartCommercialTermAsync(request.Body, ct) };
}
