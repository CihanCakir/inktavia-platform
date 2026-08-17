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
