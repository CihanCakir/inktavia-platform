using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients.CargoDry;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.RenewKit;

[DocumentationInfo("Renew kit BFF command handler", "Calls the CargoDry admin kit renew endpoint and returns the updated kit after renewal.")]
public sealed class RenewKitBffCommandHandler
    : AizenCommandHandler<RenewKitBffCommand, RenewKitBffCommandResponse>
{
    private readonly ICargoDryRemoteCall _remote;

    public RenewKitBffCommandHandler(ICargoDryRemoteCall remote)
        => _remote = remote;

    public override async Task<RenewKitBffCommandResponse?> Handle(
        RenewKitBffCommand request, CancellationToken ct)
    {
        var result = await _remote.RenewKitAsync(
            request.KitId,
            new RenewKitBffRequest
            {
                AddedDays  = request.AddedDays,
                PaymentRef = request.PaymentRef,
            }, ct);

        return new RenewKitBffCommandResponse { Kit = result };
    }
}
