using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.RefundPayment;

[DocumentationInfo("Refund payment BFF command handler",
    "Initiates a full or partial refund against a captured payment. Creates a refund record and calls the gateway refund endpoint.")]
public sealed class RefundPaymentBffCommandHandler
    : AizenCommandHandler<RefundPaymentBffCommand, RefundPaymentBffCommandResponse>
{
    private readonly IPaymentRemoteCall _remote;

    public RefundPaymentBffCommandHandler(IPaymentRemoteCall remote)
        => _remote = remote;

    public override async Task<RefundPaymentBffCommandResponse?> Handle(
        RefundPaymentBffCommand request, CancellationToken ct)
    {
        var result = await _remote.RefundAsync(request.Id, request.Body, ct);
        return new RefundPaymentBffCommandResponse { Result = result };
    }
}
