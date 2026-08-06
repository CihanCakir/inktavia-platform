using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.ReinstatePayment;

[DocumentationInfo("Reinstate payment BFF command handler",
    "Reinstates a previously cancelled transaction, returning it to an active escrow or pending state for retry flows.")]
public sealed class ReinstatePaymentBffCommandHandler
    : AizenCommandHandler<ReinstatePaymentBffCommand, ReinstatePaymentBffCommandResponse>
{
    private readonly IPaymentRemoteCall _remote;

    public ReinstatePaymentBffCommandHandler(IPaymentRemoteCall remote)
        => _remote = remote;

    public override async Task<ReinstatePaymentBffCommandResponse?> Handle(
        ReinstatePaymentBffCommand request, CancellationToken ct)
    {
        var result = await _remote.ReinstateAsync(request.Id, request.Body, ct);
        return new ReinstatePaymentBffCommandResponse { Result = result };
    }
}
