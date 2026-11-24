using Aizen.Core.CQRS.Message;
using Aizen.Modules.Notification.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Notifications.Command;

public sealed class SuppressNewsletterSubscriberBffCommand : AizenCommand<NewsletterSubscriberListItemDto>
{
    public long Id { get; init; }
}
