using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Notification.Abstraction.Response;

namespace Aizen.Bff.MarineProvider.Application.Notifications;

public sealed class MarkAllNotificationsReadBffCommandHandler
    : AizenCommandHandler<MarkAllNotificationsReadBffCommand, MarkAllNotificationsReadResponse>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityHolder _h;
    private readonly INotificationRemoteCall _c;

    public MarkAllNotificationsReadBffCommandHandler(IProviderProfileResolver r, IProviderIdentityHolder h, INotificationRemoteCall c)
    { _resolver = r; _h = h; _c = c; }

    public override async Task<MarkAllNotificationsReadResponse?> Handle(MarkAllNotificationsReadBffCommand cmd, CancellationToken ct)
    {
        await _resolver.ResolveAsync(ct);
        if (_h.ProfileId is null or 0)
            throw new AizenBusinessException("Provider identity could not be resolved.");
        return (await _c.MarkAllRead()).Body;
    }
}
