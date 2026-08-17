using Aizen.Core.Cache.Abstraction;
using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.Notification.Abstraction.Response;
using Aizen.Modules.Notification.Domain.Interface.Repository;

namespace Aizen.Modules.Notification.Application.Command.BulkMarkAsRead;

public sealed class BulkMarkAsReadCommandHandler
    : AizenCommandHandler<BulkMarkAsReadCommand, MarkAllNotificationsReadResponse>
{
    private readonly INotificationRepository _repository;
    private readonly IAizenInfoAccessor      _info;
    private readonly IAizenDistributedCache  _cache;

    public BulkMarkAsReadCommandHandler(
        INotificationRepository repository,
        IAizenInfoAccessor info,
        IAizenDistributedCache cache)
    {
        _repository = repository;
        _info       = info;
        _cache      = cache;
    }

    public override async Task<MarkAllNotificationsReadResponse?> Handle(
        BulkMarkAsReadCommand request, CancellationToken cancellationToken)
    {
        var effectiveRecipientId = ResolveEffectiveRecipientId();
        var updatedCount = await _repository.BulkMarkAsReadAsync(effectiveRecipientId, cancellationToken);

        await BumpGenerationAsync(effectiveRecipientId, cancellationToken);

        return new MarkAllNotificationsReadResponse { UpdatedCount = updatedCount };
    }

    private async Task BumpGenerationAsync(long rid, CancellationToken ct)
    {
        try
        {
            await _cache.SetAsync(
                DateTimeOffset.UtcNow.Ticks,
                $"notif:gen:{rid}",
                new AizenCacheOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) },
                ct);
        }
        catch { /* cache failure must not break the write path */ }
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
