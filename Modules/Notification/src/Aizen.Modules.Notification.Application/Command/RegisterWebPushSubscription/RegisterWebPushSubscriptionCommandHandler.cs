using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Notification.Domain.Interface.Repository;

namespace Aizen.Modules.Notification.Application.Command.RegisterWebPushSubscription;

public sealed class RegisterWebPushSubscriptionCommandHandler
    : AizenCommandHandler<RegisterWebPushSubscriptionCommand, bool>
{
    private readonly IUserDeviceTokenRepository _repository;

    public RegisterWebPushSubscriptionCommandHandler(IUserDeviceTokenRepository repository)
        => _repository = repository;

    public override async Task<bool> Handle(RegisterWebPushSubscriptionCommand request, CancellationToken ct)
    {
        await _repository.UpsertWebPushAsync(
            request.UserId, request.Endpoint, request.P256dh, request.Auth, ct);
        return true;
    }
}
