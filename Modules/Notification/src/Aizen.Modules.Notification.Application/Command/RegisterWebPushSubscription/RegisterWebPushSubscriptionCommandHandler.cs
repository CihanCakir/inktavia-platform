using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.Notification.Abstraction.Response;
using Aizen.Modules.Notification.Domain.Interface.Repository;

namespace Aizen.Modules.Notification.Application.Command.RegisterWebPushSubscription;

public sealed class RegisterWebPushSubscriptionCommandHandler
    : AizenCommandHandler<RegisterWebPushSubscriptionCommand, PushSubscriptionResponse>
{
    private readonly IUserDeviceTokenRepository _repository;
    private readonly IAizenInfoAccessor         _info;

    public RegisterWebPushSubscriptionCommandHandler(
        IUserDeviceTokenRepository repository,
        IAizenInfoAccessor info)
    {
        _repository = repository;
        _info       = info;
    }

    public override async Task<PushSubscriptionResponse?> Handle(
        RegisterWebPushSubscriptionCommand request, CancellationToken ct)
    {
        var effectiveRecipientId = ResolveEffectiveRecipientId();
        await _repository.UpsertWebPushAsync(
            effectiveRecipientId, request.Endpoint, request.P256dh, request.Auth, ct);

        return new PushSubscriptionResponse { Active = true };
    }

    private long ResolveEffectiveRecipientId()
    {
        var profileId = _info.KeycloakTokenInfoAccessor.KeycloakTokenInfo?.ProviderProfileId;
        if (profileId is > 0)
            return profileId.Value;

        var userId = _info.UserInfoAccessor.UserInfo.UserId;
        return userId > 0
            ? userId
            : throw new UnauthorizedAccessException("Cannot resolve recipient identity.");
    }
}
