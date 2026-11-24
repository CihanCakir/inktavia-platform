using Aizen.Core.CQRS.Message;
using Aizen.Modules.Notification.Abstraction.Response;

namespace Aizen.Modules.Notification.Application.Command.UnsubscribeNewsletter;

/// <summary>P1 — one-click unsubscribe via the stable unsubscribe token. Idempotent: unsubscribing twice succeeds.</summary>
public sealed class UnsubscribeNewsletterCommand : AizenCommand<NewsletterUnsubscribeResponse>
{
    public string Token { get; init; } = default!;
}
