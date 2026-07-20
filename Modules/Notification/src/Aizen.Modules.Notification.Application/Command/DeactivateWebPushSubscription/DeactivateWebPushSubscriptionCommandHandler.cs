using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Notification.Abstraction.Response;
using Aizen.Modules.Notification.Domain.Interface.Repository;

namespace Aizen.Modules.Notification.Application.Command.DeactivateWebPushSubscription;

public sealed class DeactivateWebPushSubscriptionCommandHandler
    : AizenCommandHandler<DeactivateWebPushSubscriptionCommand, PushSubscriptionResponse>
{
    private readonly IUserDeviceTokenRepository _repository;

    public DeactivateWebPushSubscriptionCommandHandler(IUserDeviceTokenRepository repository)
        => _repository = repository;

    public override async Task<PushSubscriptionResponse?> Handle(
        DeactivateWebPushSubscriptionCommand request, CancellationToken ct)
    {
        await _repository.DeactivateByEndpointAsync(request.Endpoint, ct);
        return new PushSubscriptionResponse { Active = false };
    }
}
