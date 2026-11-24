using Aizen.Core.CQRS.Message;
using Aizen.Modules.Notification.Abstraction.Dto;
using Aizen.Modules.Notification.Abstraction.Enum;

namespace Aizen.Bff.AdminPanel.Application.Notifications.Query;

public sealed class GetNewsletterSubscribersBffQuery : AizenQuery<NewsletterSubscriberListResult>
{
    public NewsletterSubscriberStatus? Status   { get; init; }
    public string?                     Lang     { get; init; }
    public string?                     Search   { get; init; }
    public DateTimeOffset?             From     { get; init; }
    public DateTimeOffset?             To       { get; init; }
    public int                         Page     { get; init; } = 1;
    public int                         PageSize { get; init; } = 20;
}
