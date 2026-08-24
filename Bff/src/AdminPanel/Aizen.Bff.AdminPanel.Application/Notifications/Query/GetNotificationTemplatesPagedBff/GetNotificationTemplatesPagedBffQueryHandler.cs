using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Notification.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Notifications.Query;

[DocumentationInfo("Get admin notification templates paged query handler",
    "Notification modülünün sayfalı admin template listesini proxy eder.")]
public sealed class GetNotificationTemplatesPagedBffQueryHandler
    : AizenQueryHandler<GetNotificationTemplatesPagedBffQuery, NotificationTemplateListResult>
{
    private readonly INotificationTemplateRemoteCall _notification;

    public GetNotificationTemplatesPagedBffQueryHandler(INotificationTemplateRemoteCall notification)
        => _notification = notification;

    public override async Task<NotificationTemplateListResult> Handle(
        GetNotificationTemplatesPagedBffQuery request, CancellationToken cancellationToken)
    {
        var result = await _notification.GetTemplatesPaged(
            request.Channel, request.Locale, request.Status, request.Enabled, request.Search,
            request.Page, request.PageSize);

        // Gövde null gelirse sözleşmeyi koruyacak boş sayfa (Files handler ile aynı yaklaşım).
        return result?.Body ?? new NotificationTemplateListResult
        {
            Items      = new List<NotificationTemplateListItemDto>(),
            TotalCount = 0,
            Page       = request.Page,
            PageSize   = request.PageSize,
        };
    }
}
