using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Notification.Abstraction.Response;

namespace Aizen.Bff.MarineProvider.Application.Notifications;

public sealed class GetVapidPublicKeyBffQueryHandler
    : AizenQueryHandler<GetVapidPublicKeyBffQuery, VapidPublicKeyResponse>
{
    private readonly INotificationRemoteCall _c;

    public GetVapidPublicKeyBffQueryHandler(INotificationRemoteCall c) => _c = c;

    public override async Task<VapidPublicKeyResponse?> Handle(GetVapidPublicKeyBffQuery q, CancellationToken ct)
        => (await _c.GetVapidPublicKey()).Body;
}
