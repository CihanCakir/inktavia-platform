using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Notification.Abstraction.Dto;
using Aizen.Modules.Notification.Domain.Interface.Repository;

namespace Aizen.Modules.Notification.Application.Query.GetUserNotifications;

public sealed class GetUserNotificationsQueryHandler
    : AizenQueryHandler<GetUserNotificationsQuery, GetUserNotificationsResponse>
{
    private readonly INotificationRepository _repository;

    public GetUserNotificationsQueryHandler(INotificationRepository repository)
        => _repository = repository;

    public override async Task<GetUserNotificationsResponse> Handle(
        GetUserNotificationsQuery request, CancellationToken cancellationToken)
    {
        var items       = await _repository.GetByRecipientAsync(request.UserId, request.Skip, request.Take, cancellationToken);
        var unreadCount = await _repository.GetUnreadCountAsync(request.UserId, cancellationToken);

        return new GetUserNotificationsResponse
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
            Total       = items.Count,
            UnreadCount = unreadCount,
        };
    }
}
