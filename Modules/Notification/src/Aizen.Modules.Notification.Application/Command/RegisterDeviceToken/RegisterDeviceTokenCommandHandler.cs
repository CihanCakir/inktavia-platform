using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.Notification.Abstraction.Response;
using Aizen.Modules.Notification.Domain.Interface.Repository;

namespace Aizen.Modules.Notification.Application.Command.RegisterDeviceToken;

public sealed class RegisterDeviceTokenCommandHandler
    : AizenCommandHandler<RegisterDeviceTokenCommand, DeviceTokenResponse>
{
    private readonly IUserDeviceTokenRepository _repository;
    private readonly IAizenInfoAccessor         _info;

    public RegisterDeviceTokenCommandHandler(
        IUserDeviceTokenRepository repository,
        IAizenInfoAccessor info)
    {
        _repository = repository;
        _info       = info;
    }

    public override async Task<DeviceTokenResponse?> Handle(
        RegisterDeviceTokenCommand request, CancellationToken cancellationToken)
    {
        var effectiveRecipientId = ResolveEffectiveRecipientId();
        await _repository.UpsertAsync(effectiveRecipientId, request.DeviceToken, request.Platform, cancellationToken);

        return new DeviceTokenResponse { Registered = true };
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
