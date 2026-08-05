using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Notification.Abstraction.Request;
using Aizen.Modules.Notification.Abstraction.Response;

namespace Aizen.Bff.MarineProvider.Application.Notifications;

public sealed class UpdateNotificationPreferenceBffCommandHandler
    : AizenCommandHandler<UpdateNotificationPreferenceBffCommand, NotificationPreferencesResponse>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityHolder _h;
    private readonly INotificationRemoteCall _c;

    public UpdateNotificationPreferenceBffCommandHandler(
        IProviderProfileResolver r, IProviderIdentityHolder h, INotificationRemoteCall c)
    { _resolver = r; _h = h; _c = c; }

    public override async Task<NotificationPreferencesResponse?> Handle(
        UpdateNotificationPreferenceBffCommand request, CancellationToken ct)
    {
        await _resolver.ResolveAsync(ct);
        if (_h.ProfileId is null or 0)
            throw new AizenBusinessException("Provider identity could not be resolved.");

        return (await _c.UpdatePreference(new UpdateNotificationPreferenceRequest
        {
            Category = request.Category,
            Channel  = request.Channel,
            Enabled  = request.Enabled,
        })).Body;
    }
}
