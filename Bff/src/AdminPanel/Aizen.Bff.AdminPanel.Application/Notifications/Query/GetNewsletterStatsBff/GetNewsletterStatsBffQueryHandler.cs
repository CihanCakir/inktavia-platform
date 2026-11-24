using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Notification.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Notifications.Query;

[DocumentationInfo("Get newsletter stats query handler", "E-bülten gösterge sayaçlarını proxy eder.")]
public sealed class GetNewsletterStatsBffQueryHandler
    : AizenQueryHandler<GetNewsletterStatsBffQuery, NewsletterStatsDto>
{
    private readonly INotificationRemoteCall _notification;

    public GetNewsletterStatsBffQueryHandler(INotificationRemoteCall notification)
        => _notification = notification;

    public override async Task<NewsletterStatsDto> Handle(
        GetNewsletterStatsBffQuery request, CancellationToken cancellationToken)
    {
        var result = await _notification.GetNewsletterStatsAsync(cancellationToken);
        return result?.Body ?? new NewsletterStatsDto();
    }
}
