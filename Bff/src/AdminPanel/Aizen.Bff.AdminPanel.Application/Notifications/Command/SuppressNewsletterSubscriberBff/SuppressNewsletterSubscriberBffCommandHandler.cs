using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Notification.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Notifications.Command;

[DocumentationInfo("Suppress newsletter subscriber command handler",
    "Bir e-bülten abonesini manuel olarak baskılar (suppress) — Notification modülüne proxy.")]
public sealed class SuppressNewsletterSubscriberBffCommandHandler
    : AizenCommandHandler<SuppressNewsletterSubscriberBffCommand, NewsletterSubscriberListItemDto>
{
    private readonly INotificationRemoteCall _notification;

    public SuppressNewsletterSubscriberBffCommandHandler(INotificationRemoteCall notification)
        => _notification = notification;

    public override async Task<NewsletterSubscriberListItemDto?> Handle(
        SuppressNewsletterSubscriberBffCommand request, CancellationToken cancellationToken)
    {
        var result = await _notification.SuppressNewsletterSubscriberAsync(request.Id, cancellationToken);
        return result?.Body;
    }
}
