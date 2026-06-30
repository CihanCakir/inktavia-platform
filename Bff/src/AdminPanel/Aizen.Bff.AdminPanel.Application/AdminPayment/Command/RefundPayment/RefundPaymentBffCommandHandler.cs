using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Command.RefundPayment;

[DocumentationInfo("Refund payment BFF command handler",
    "Initiates a full or partial refund against a captured payment. Creates a refund record and calls the gateway refund endpoint.")]
public sealed class RefundPaymentBffCommandHandler
    : AizenCommandHandler<RefundPaymentBffCommand, RefundPaymentBffCommandResponse>
{
    private readonly IAdminPaymentBffRemoteCall _remote;

    public RefundPaymentBffCommandHandler(IAdminPaymentBffRemoteCall remote)
        => _remote = remote;

    public override async Task<RefundPaymentBffCommandResponse?> Handle(
        RefundPaymentBffCommand request, CancellationToken ct)
    {
        var result = await _remote.RefundAsync(request.Id, request.Body, ct);
        return new RefundPaymentBffCommandResponse { Result = result };
    }
}
