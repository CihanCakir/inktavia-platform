using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Notification.Abstraction.Dto;
using Aizen.Modules.Notification.Domain.Interface.Repository;

namespace Aizen.Modules.Notification.Application.Query.GetNotificationHistoryDetail;

public sealed class GetNotificationHistoryDetailQueryHandler
    : AizenQueryHandler<GetNotificationHistoryDetailQuery, NotificationHistoryDetailDto>
{
    private readonly INotificationRepository _repository;

    public GetNotificationHistoryDetailQueryHandler(INotificationRepository repository)
        => _repository = repository;

    public override async Task<NotificationHistoryDetailDto?> Handle(
        GetNotificationHistoryDetailQuery request, CancellationToken cancellationToken)
    {
        var n = await _repository.GetByIdAsync(request.Id, cancellationToken);
        // Bulunamazsa null → controller AizenApiResponse<...?> ile boş gövde döner (iş hatası değil, sadece yok).
        if (n is null)
            return null;

        return new NotificationHistoryDetailDto
        {
            Id                  = n.Id,
            RecipientUserId     = n.RecipientUserId,
            Type                = n.Type,
            Channel             = n.Channel,
            TemplateCode        = n.TemplateCode,
            Title               = n.Title,
            Locale              = n.Locale,
            Status              = n.Status,
            CampaignId          = n.CampaignId,
            CreatedAt           = n.CreatedAt,
            SentAt              = n.SentAt,
            ReadAt              = n.ReadAt,
            Body                = n.Body,
            DeepLink            = n.DeepLink,
            DeliveryProviderRef = n.DeliveryProviderRef,
            ReferenceType       = n.ReferenceType,
            ReferenceId         = n.ReferenceId,
            MetadataJson        = n.MetadataJson,
        };
    }
}
