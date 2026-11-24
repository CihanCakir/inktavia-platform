using Aizen.Core.CQRS.Message;
using Aizen.Modules.Notification.Abstraction.Response;

namespace Aizen.Bff.Marine.Web.Application.Newsletter.Command.UnsubscribeWebNewsletter;

/// <summary>POST /api/v1/web/newsletter/unsubscribe — forwards an unsubscribe token to the Notification module.</summary>
public sealed class UnsubscribeWebNewsletterCommand : AizenCommand<NewsletterUnsubscribeResponse>
{
    public string Token { get; init; } = default!;
}
