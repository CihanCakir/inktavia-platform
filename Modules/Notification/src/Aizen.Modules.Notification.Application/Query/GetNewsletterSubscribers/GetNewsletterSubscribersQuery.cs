using Aizen.Core.CQRS.Message;
using Aizen.Modules.Notification.Abstraction.Dto;
using Aizen.Modules.Notification.Abstraction.Enum;

namespace Aizen.Modules.Notification.Application.Query.GetNewsletterSubscribers;

/// <summary>P1 — admin paged subscriber list. All filters optional and combinable; newest first.</summary>
public sealed class GetNewsletterSubscribersQuery : AizenQuery<NewsletterSubscriberListResult>
{
    public NewsletterSubscriberStatus? Status   { get; init; }
    public string?                     Lang     { get; init; }
    public string?                     Search   { get; init; }
    public DateTimeOffset?             From     { get; init; }
    public DateTimeOffset?             To       { get; init; }
    public int                         Page     { get; init; } = 1;
    public int                         PageSize { get; init; } = 20;
}
