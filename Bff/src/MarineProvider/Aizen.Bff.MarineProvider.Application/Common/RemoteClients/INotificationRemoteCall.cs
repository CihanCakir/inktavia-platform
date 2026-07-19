using Aizen.Core.Infrastructure.Api;
using Aizen.Core.RemoteCall.Abstraction;

namespace Aizen.Bff.MarineProvider.Application.Common.RemoteClients;

public interface INotificationRemoteCall : IAizenRemoteCall
{
    [AizenRemoteCallPost("/api/v1/notification/notifications/push-subscriptions")]
    Task<AizenApiResponse<object>> RegisterWebPushSubscription(
        [AizenRemoteCallBody] RegisterWebPushSubscriptionBffRequest body);

    [AizenRemoteCallDelete("/api/v1/notification/notifications/push-subscriptions")]
    Task<AizenApiResponse<object>> DeactivateWebPushSubscription(
        [AizenRemoteCallBody] DeactivateWebPushSubscriptionBffRequest body);
}

public sealed class RegisterWebPushSubscriptionBffRequest
{
    public string Endpoint { get; set; } = default!;
    public string P256dh { get; set; } = default!;
    public string Auth { get; set; } = default!;
}

public sealed class DeactivateWebPushSubscriptionBffRequest
{
    public string Endpoint { get; set; } = default!;
}
