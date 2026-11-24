using Aizen.Core.CQRS.Message;
using Aizen.Modules.Notification.Abstraction.Response;

namespace Aizen.Bff.Marine.Web.Application.Newsletter.Command.ConfirmWebNewsletter;

/// <summary>POST /api/v1/web/newsletter/confirm — forwards a confirmation token to the Notification module.</summary>
public sealed class ConfirmWebNewsletterCommand : AizenCommand<NewsletterConfirmResponse>
{
    public string Token { get; init; } = default!;
}
