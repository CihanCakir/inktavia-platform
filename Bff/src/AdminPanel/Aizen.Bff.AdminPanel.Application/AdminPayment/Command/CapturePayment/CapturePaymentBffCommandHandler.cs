using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Command.CapturePayment;

[DocumentationInfo("Capture payment BFF command handler",
    "Captures a previously held escrow payment, moving it from reserved to captured state and triggering provider payout scheduling.")]
public sealed class CapturePaymentBffCommandHandler
    : AizenCommandHandler<CapturePaymentBffCommand, CapturePaymentBffCommandResponse>
{
    private readonly IPaymentRemoteCall _remote;

    public CapturePaymentBffCommandHandler(IPaymentRemoteCall remote)
        => _remote = remote;

    public override async Task<CapturePaymentBffCommandResponse?> Handle(
        CapturePaymentBffCommand request, CancellationToken ct)
    {
        var result = await _remote.CaptureAsync(request.Id, request.Body, ct);
        return new CapturePaymentBffCommandResponse { Result = result };
    }
}
