using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.ReversePartialRefund;

[DocumentationInfo("Reverse partial refund BFF command handler",
    "Reverses a previously issued partial refund record. Used to correct erroneous refunds before gateway settlement.")]
public sealed class ReversePartialRefundBffCommandHandler
    : AizenCommandHandler<ReversePartialRefundBffCommand, ReversePartialRefundBffCommandResponse>
{
    private readonly IPaymentRemoteCall _remote;

    public ReversePartialRefundBffCommandHandler(IPaymentRemoteCall remote)
        => _remote = remote;

    public override async Task<ReversePartialRefundBffCommandResponse?> Handle(
        ReversePartialRefundBffCommand request, CancellationToken ct)
    {
        var result = await _remote.ReverseRefundAsync(request.RefundRecordId, request.Body, ct);
        return new ReversePartialRefundBffCommandResponse { Result = result };
    }
}
