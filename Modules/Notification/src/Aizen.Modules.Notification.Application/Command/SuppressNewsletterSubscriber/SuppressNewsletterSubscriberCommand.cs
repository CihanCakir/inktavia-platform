using Aizen.Core.CQRS.Message;
using Aizen.Modules.Notification.Abstraction.Dto;

namespace Aizen.Modules.Notification.Application.Command.SuppressNewsletterSubscriber;

/// <summary>P1 — admin manual suppression of a subscriber (abuse/complaint). Sticky; audited via log. Returns the
/// updated row (null when the id is unknown).</summary>
public sealed class SuppressNewsletterSubscriberCommand : AizenCommand<NewsletterSubscriberListItemDto>
{
    public long Id { get; init; }
}
