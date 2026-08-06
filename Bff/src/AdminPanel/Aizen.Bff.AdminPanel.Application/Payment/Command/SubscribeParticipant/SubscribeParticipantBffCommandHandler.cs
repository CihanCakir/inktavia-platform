using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.SubscribeParticipant;

[DocumentationInfo("Subscribe participant BFF command handler",
    "Enrolls a participant profile in a subscription plan, activating discounts, CargoDry benefits, and InkCoin earn multiplier.")]
public sealed class SubscribeParticipantBffCommandHandler
    : AizenCommandHandler<SubscribeParticipantBffCommand, SubscribeParticipantBffCommandResponse>
{
    private readonly IPaymentRemoteCall _remote;

    public SubscribeParticipantBffCommandHandler(IPaymentRemoteCall remote)
        => _remote = remote;

    public override async Task<SubscribeParticipantBffCommandResponse?> Handle(
        SubscribeParticipantBffCommand request, CancellationToken ct)
    {
        var result = await _remote.SubscribeParticipantAsync(request.Body, ct);
        return new SubscribeParticipantBffCommandResponse { Result = result };
    }
}
