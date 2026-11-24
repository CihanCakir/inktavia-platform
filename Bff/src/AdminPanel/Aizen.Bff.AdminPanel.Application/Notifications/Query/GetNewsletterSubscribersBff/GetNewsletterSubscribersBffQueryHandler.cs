using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Notification.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Notifications.Query;

[DocumentationInfo("Get newsletter subscribers paged query handler",
    "Notification modülünün sayfalı e-bülten abone listesini proxy eder.")]
public sealed class GetNewsletterSubscribersBffQueryHandler
    : AizenQueryHandler<GetNewsletterSubscribersBffQuery, NewsletterSubscriberListResult>
{
    private readonly INotificationRemoteCall _notification;

    public GetNewsletterSubscribersBffQueryHandler(INotificationRemoteCall notification)
        => _notification = notification;

    public override async Task<NewsletterSubscriberListResult> Handle(
        GetNewsletterSubscribersBffQuery request, CancellationToken cancellationToken)
    {
        var result = await _notification.GetNewsletterSubscribersAsync(
            request.Status, request.Lang, request.Search, request.From, request.To,
            request.Page, request.PageSize, cancellationToken);

        return result?.Body ?? new NewsletterSubscriberListResult
        {
            Items      = new List<NewsletterSubscriberListItemDto>(),
            TotalCount = 0,
            Page       = request.Page,
            PageSize   = request.PageSize,
        };
    }
}
