using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.ReleasePaymentEscrow;

[DocumentationInfo("Release payment escrow BFF command handler",
    "Releases funds held in escrow back to the payer. Used for cancellation flows or dispute resolutions where the provider is not paid.")]
public sealed class ReleasePaymentEscrowBffCommandHandler
    : AizenCommandHandler<ReleasePaymentEscrowBffCommand, ReleasePaymentEscrowBffCommandResponse>
{
    private readonly IPaymentRemoteCall _remote;

    public ReleasePaymentEscrowBffCommandHandler(IPaymentRemoteCall remote)
        => _remote = remote;

    public override async Task<ReleasePaymentEscrowBffCommandResponse?> Handle(
        ReleasePaymentEscrowBffCommand request, CancellationToken ct)
    {
        var result = await _remote.ReleaseEscrowAsync(request.Id, request.Body, ct);
        return new ReleasePaymentEscrowBffCommandResponse { Result = result };
    }
}
