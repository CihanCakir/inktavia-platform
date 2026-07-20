using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Notification.Abstraction.Response;

namespace Aizen.Bff.MarineProvider.Application.Notifications;

public sealed class SubscribePushCommandHandler
    : AizenCommandHandler<SubscribePushCommand, PushSubscriptionResponse>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityHolder _identityHolder;
    private readonly INotificationRemoteCall _notification;

    public SubscribePushCommandHandler(
        IProviderProfileResolver resolver,
        IProviderIdentityHolder identityHolder,
        INotificationRemoteCall notification)
    {
        _resolver = resolver;
        _identityHolder = identityHolder;
        _notification = notification;
    }

    public override async Task<PushSubscriptionResponse?> Handle(SubscribePushCommand request, CancellationToken ct)
    {
        await _resolver.ResolveAsync(ct);

        if (_identityHolder.UserId is null or 0)
            throw new AizenBusinessException("Provider identity could not be resolved.");

        return (await _notification.RegisterWebPushSubscription(new RegisterWebPushSubscriptionBffRequest
        {
            Endpoint = request.Endpoint,
            P256dh = request.P256dh,
            Auth = request.Auth,
        })).Body;
    }
}
