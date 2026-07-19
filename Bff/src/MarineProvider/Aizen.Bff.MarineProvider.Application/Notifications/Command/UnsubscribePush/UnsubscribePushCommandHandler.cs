using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;

namespace Aizen.Bff.MarineProvider.Application.Notifications;

public sealed class UnsubscribePushCommandHandler
    : AizenCommandHandler<UnsubscribePushCommand, UnsubscribePushResponse>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityHolder _identityHolder;
    private readonly INotificationRemoteCall _notification;

    public UnsubscribePushCommandHandler(
        IProviderProfileResolver resolver,
        IProviderIdentityHolder identityHolder,
        INotificationRemoteCall notification)
    {
        _resolver = resolver;
        _identityHolder = identityHolder;
        _notification = notification;
    }

    public override async Task<UnsubscribePushResponse?> Handle(UnsubscribePushCommand request, CancellationToken ct)
    {
        await _resolver.ResolveAsync(ct);

        if (_identityHolder.UserId is null or 0)
            throw new AizenBusinessException("Provider identity could not be resolved.");

        await _notification.DeactivateWebPushSubscription(new DeactivateWebPushSubscriptionBffRequest
        {
            Endpoint = request.Endpoint,
        });

        return new UnsubscribePushResponse { Success = true };
    }
}
