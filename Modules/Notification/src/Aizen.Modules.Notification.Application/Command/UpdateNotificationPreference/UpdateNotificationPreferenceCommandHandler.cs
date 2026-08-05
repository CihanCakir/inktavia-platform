using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Notification.Abstraction;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Abstraction.Response;
using Aizen.Modules.Notification.Application.Services;
using Aizen.Modules.Notification.Domain.Interface.Repository;

namespace Aizen.Modules.Notification.Application.Command.UpdateNotificationPreference;

public sealed class UpdateNotificationPreferenceCommandHandler
    : AizenCommandHandler<UpdateNotificationPreferenceCommand, NotificationPreferencesResponse>
{
    private readonly INotificationPreferenceRepository _repository;
    private readonly IAizenInfoAccessor               _info;

    public UpdateNotificationPreferenceCommandHandler(
        INotificationPreferenceRepository repository,
        IAizenInfoAccessor info)
    {
        _repository = repository;
        _info       = info;
    }

    public override async Task<NotificationPreferencesResponse?> Handle(
        UpdateNotificationPreferenceCommand request, CancellationToken ct)
    {
        if (!Enum.TryParse<NotificationCategory>(request.Category, ignoreCase: true, out var category))
            throw new AizenBusinessException($"Unknown notification category '{request.Category}'.");

        if (!Enum.TryParse<NotificationChannel>(request.Channel, ignoreCase: true, out var channel))
            throw new AizenBusinessException($"Unknown notification channel '{request.Channel}'.");

        // Only Push/Email are user-changeable; InApp and the security Account category are locked.
        if (channel is not (NotificationChannel.Push or NotificationChannel.Email))
            throw new AizenBusinessException($"Channel '{channel}' is not user-configurable.");

        if (NotificationPreferencePolicy.IsLocked(category, channel))
            throw new AizenBusinessException($"The '{category}/{channel}' preference is locked and cannot be changed.");

        var userId = ResolveEffectiveRecipientId();
        await _repository.UpsertAsync(userId, category, channel, request.Enabled, ct);

        var stored = await _repository.GetByUserAsync(userId, ct);
        return NotificationPreferenceResponseBuilder.Build(stored);
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
