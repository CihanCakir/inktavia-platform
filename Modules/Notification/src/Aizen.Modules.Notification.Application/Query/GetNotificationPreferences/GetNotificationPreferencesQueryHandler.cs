using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.Notification.Abstraction.Response;
using Aizen.Modules.Notification.Application.Services;
using Aizen.Modules.Notification.Domain.Interface.Repository;

namespace Aizen.Modules.Notification.Application.Query.GetNotificationPreferences;

public sealed class GetNotificationPreferencesQueryHandler
    : AizenQueryHandler<GetNotificationPreferencesQuery, NotificationPreferencesResponse>
{
    private readonly INotificationPreferenceRepository _repository;
    private readonly IAizenInfoAccessor               _info;

    public GetNotificationPreferencesQueryHandler(
        INotificationPreferenceRepository repository,
        IAizenInfoAccessor info)
    {
        _repository = repository;
        _info       = info;
    }

    public override async Task<NotificationPreferencesResponse> Handle(
        GetNotificationPreferencesQuery request, CancellationToken ct)
    {
        var userId = ResolveEffectiveRecipientId();
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
