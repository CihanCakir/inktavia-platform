using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Notification;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Infrastructure.Exception;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Notification;

/// <summary>GET /api/v1/mobile/notifications — the caller's notification inbox (paged) + the unread badge count.
/// The module scopes to the caller's participant from the assertion. Cost-free.</summary>
public sealed class GetMobileNotificationsQuery : AizenQuery<MobileNotificationListDto>
{
    public GetMobileNotificationsQuery(int skip, int take)
    {
        Skip = skip < 0 ? 0 : skip;
        Take = take is <= 0 or > 100 ? 20 : take;
    }

    public int Skip { get; }
    public int Take { get; }
}

public sealed class GetMobileNotificationsQueryHandler
    : AizenQueryHandler<GetMobileNotificationsQuery, MobileNotificationListDto>
{
    private readonly IParticipantProfileResolver _resolver;
    private readonly INotificationRemoteCall _notification;

    public GetMobileNotificationsQueryHandler(IParticipantProfileResolver resolver, INotificationRemoteCall notification)
    {
        _resolver = resolver;
        _notification = notification;
    }

    public override async Task<MobileNotificationListDto?> Handle(
        GetMobileNotificationsQuery request, CancellationToken cancellationToken)
    {
        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.ProfileId is not > 0)
            throw new AizenBusinessException("No participant profile is linked to this account yet.");

        var resp = await _notification.GetUserNotifications(request.Skip, request.Take);
        return MobileNotificationMapper.MapList(resp?.Body);
    }
}
