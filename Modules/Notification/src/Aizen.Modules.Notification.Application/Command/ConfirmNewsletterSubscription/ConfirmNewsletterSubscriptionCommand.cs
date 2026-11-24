using Aizen.Core.CQRS.Message;
using Aizen.Modules.Notification.Abstraction.Response;

namespace Aizen.Modules.Notification.Application.Command.ConfirmNewsletterSubscription;

/// <summary>P1 — redeem a confirmation token (double opt-in). Single-use, expiry-checked, constant-time compared.</summary>
public sealed class ConfirmNewsletterSubscriptionCommand : AizenCommand<NewsletterConfirmResponse>
{
    public string Token { get; init; } = default!;
}
