using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Notification.Abstraction.Response;

namespace Aizen.Bff.MarineProvider.Application.Notifications;

public sealed class GetProviderNotificationsBffQueryHandler
    : AizenQueryHandler<GetProviderNotificationsBffQuery, NotificationListResponse>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityHolder _h;
    private readonly INotificationRemoteCall _c;

    public GetProviderNotificationsBffQueryHandler(IProviderProfileResolver r, IProviderIdentityHolder h, INotificationRemoteCall c)
    { _resolver = r; _h = h; _c = c; }

    public override async Task<NotificationListResponse?> Handle(GetProviderNotificationsBffQuery q, CancellationToken ct)
    {
        await _resolver.ResolveAsync(ct);
        if (_h.ProfileId is null or 0)
            throw new AizenBusinessException("Provider identity could not be resolved.");
        return (await _c.GetNotifications(q.Skip, q.Take)).Body;
    }
}
