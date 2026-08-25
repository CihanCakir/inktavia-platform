using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Notification.Abstraction.Dto;
using Aizen.Modules.Notification.Domain.Interface.Repository;

namespace Aizen.Modules.Notification.Application.Query.GetNotificationHistoryPaged;

public sealed class GetNotificationHistoryPagedQueryHandler
    : AizenQueryHandler<GetNotificationHistoryPagedQuery, NotificationHistoryListResult>
{
    private readonly INotificationRepository _repository;

    public GetNotificationHistoryPagedQueryHandler(INotificationRepository repository)
        => _repository = repository;

    public override async Task<NotificationHistoryListResult> Handle(
        GetNotificationHistoryPagedQuery request, CancellationToken cancellationToken)
    {
        // Sayfa/boyut güvenli aralığa çekilir (1 tabanlı sayfa, makul üst sınır) — GetNotificationTemplatesPaged ile aynı.
        var page     = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 200 ? 20 : request.PageSize;
        var skip     = (page - 1) * pageSize;

        var (items, total) = await _repository.GetHistoryPagedAsync(
            request.From, request.To, request.Channel, request.Status,
            request.TemplateCode, request.RecipientUserId, request.CampaignId, skip, pageSize, cancellationToken);

        return new NotificationHistoryListResult
        {
            Items = items.Select(n => new NotificationHistoryListItemDto
            {
                Id              = n.Id,
                RecipientUserId = n.RecipientUserId,
                Type            = n.Type,
                Channel         = n.Channel,
                TemplateCode    = n.TemplateCode,
                Title           = n.Title,
                Locale          = n.Locale,
                Status          = n.Status,
                CampaignId      = n.CampaignId,
                CreatedAt       = n.CreatedAt,
                SentAt          = n.SentAt,
                ReadAt          = n.ReadAt,
            }).ToList(),
            TotalCount = total,
            Page       = page,
            PageSize   = pageSize,
        };
    }
}
