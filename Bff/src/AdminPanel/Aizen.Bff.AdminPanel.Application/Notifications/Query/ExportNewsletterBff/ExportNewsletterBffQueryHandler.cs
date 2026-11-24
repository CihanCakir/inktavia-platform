using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.Notifications.Query;

[DocumentationInfo("Export newsletter subscribers CSV query handler",
    "Onaylı (Confirmed) e-bülten abonelerinin CSV dışa aktarımını proxy eder.")]
public sealed class ExportNewsletterBffQueryHandler
    : AizenQueryHandler<ExportNewsletterBffQuery, string>
{
    private readonly INotificationRemoteCall _notification;

    public ExportNewsletterBffQueryHandler(INotificationRemoteCall notification)
        => _notification = notification;

    public override async Task<string> Handle(ExportNewsletterBffQuery request, CancellationToken cancellationToken)
        => await _notification.ExportNewsletterAsync(cancellationToken) ?? string.Empty;
}
