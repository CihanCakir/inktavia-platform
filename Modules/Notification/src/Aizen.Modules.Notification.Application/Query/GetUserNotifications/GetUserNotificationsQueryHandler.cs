using Aizen.Core.Cache.Abstraction;
using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.Notification.Abstraction.Dto;
using Aizen.Modules.Notification.Abstraction.Response;
using Aizen.Modules.Notification.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Application.Query.GetUserNotifications;

public sealed class GetUserNotificationsQueryHandler
    : AizenQueryHandler<GetUserNotificationsQuery, NotificationListResponse>
{
    private readonly INotificationRepository _repository;
    private readonly IAizenInfoAccessor      _info;
    private readonly IAizenDistributedCache  _cache;
    private readonly ILogger<GetUserNotificationsQueryHandler> _logger;

    private static readonly AizenCacheOptions PageCacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60),
    };

    private static readonly AizenCacheOptions GenerationCacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
    };

    public GetUserNotificationsQueryHandler(
        INotificationRepository repository,
        IAizenInfoAccessor info,
        IAizenDistributedCache cache,
        ILogger<GetUserNotificationsQueryHandler> logger)
    {
        _repository = repository;
        _info       = info;
        _cache      = cache;
        _logger     = logger;
    }

    public override async Task<NotificationListResponse> Handle(
        GetUserNotificationsQuery request, CancellationToken cancellationToken)
    {
        var rid = ResolveEffectiveRecipientId();

        try
        {
            var genKey = $"notif:gen:{rid}";
            var (genExists, gen) = await _cache.TryGetAsync<long>(genKey, cancellationToken);
            if (!genExists)
            {
                gen = DateTimeOffset.UtcNow.Ticks;
                await _cache.SetAsync(gen, genKey, GenerationCacheOptions, cancellationToken);
            }

            var pageKey = $"notif:list:{rid}:v{gen}:{request.Skip}:{request.Take}";
            var (hit, cached) = await _cache.TryGetAsync<NotificationListResponse>(pageKey, cancellationToken);
            if (hit && cached is not null)
                return cached;

            var result = await FetchFromDb(rid, request.Skip, request.Take, cancellationToken);
            await _cache.SetAsync(result, pageKey, PageCacheOptions, cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Notification cache read failed for rid={Rid}; falling back to DB.", rid);
            return await FetchFromDb(rid, request.Skip, request.Take, cancellationToken);
        }
    }

    private async Task<NotificationListResponse> FetchFromDb(
        long rid, int skip, int take, CancellationToken ct)
    {
        var items       = await _repository.GetByRecipientAsync(rid, skip, take, ct);
        var total       = await _repository.CountByRecipientAsync(rid, ct);
        var unreadCount = await _repository.GetUnreadCountAsync(rid, ct);

        return new NotificationListResponse
        {
            Items = items.Select(n => new NotificationDto
            {
                Id           = n.Id,
                Type         = n.Type,
                Channel      = n.Channel,
                Status       = n.Status,
                Title        = n.Title,
                Body         = n.Body,
                MetadataJson = n.MetadataJson,
                IsRead       = n.IsRead,
                CreatedAt    = n.CreatedAt,
                ReadAt       = n.ReadAt,
            }).ToList(),
            Total       = total,
            UnreadCount = unreadCount,
        };
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
