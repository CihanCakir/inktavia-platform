using Aizen.Bff.Marine.Web.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Notification.Abstraction.Request;
using Aizen.Modules.Notification.Abstraction.Response;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Web.Application.Newsletter.Command.UnsubscribeWebNewsletter;

public sealed class UnsubscribeWebNewsletterCommandHandler
    : AizenCommandHandler<UnsubscribeWebNewsletterCommand, NewsletterUnsubscribeResponse>
{
    private readonly INotificationRemoteCall _notification;
    private readonly ILogger<UnsubscribeWebNewsletterCommandHandler> _logger;

    public UnsubscribeWebNewsletterCommandHandler(
        INotificationRemoteCall notification, ILogger<UnsubscribeWebNewsletterCommandHandler> logger)
    {
        _notification = notification;
        _logger = logger;
    }

    public override async Task<NewsletterUnsubscribeResponse?> Handle(
        UnsubscribeWebNewsletterCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var resp = await _notification.UnsubscribeNewsletter(new UnsubscribeNewsletterRequest { Token = request.Token });
            return resp?.Body ?? new NewsletterUnsubscribeResponse { Unsubscribed = false };
        }
        catch (Refit.ApiException ex)
        {
            _logger.LogWarning(ex, "Newsletter unsubscribe failed (status {Status}).", ex.StatusCode);
            throw new AizenBusinessException("Could not process your unsubscribe right now. Please try again later.");
        }
    }
}
