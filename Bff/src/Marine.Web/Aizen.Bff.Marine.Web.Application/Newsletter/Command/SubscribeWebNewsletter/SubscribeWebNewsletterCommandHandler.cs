using Aizen.Bff.Marine.Web.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Notification.Abstraction.Request;
using Aizen.Modules.Notification.Abstraction.Response;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Web.Application.Newsletter.Command.SubscribeWebNewsletter;

public sealed class SubscribeWebNewsletterCommandHandler
    : AizenCommandHandler<SubscribeWebNewsletterCommand, NewsletterSubscribeResponse>
{
    private readonly INotificationRemoteCall _notification;
    private readonly ILogger<SubscribeWebNewsletterCommandHandler> _logger;

    public SubscribeWebNewsletterCommandHandler(
        INotificationRemoteCall notification, ILogger<SubscribeWebNewsletterCommandHandler> logger)
    {
        _notification = notification;
        _logger = logger;
    }

    public override async Task<NewsletterSubscribeResponse?> Handle(
        SubscribeWebNewsletterCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Forward the untrusted payload; the module owns idempotency/spam/persistence/mail. The caller IP goes in
            // X-Forwarded-For (the module hashes it) — the browser never sends its own IP.
            var resp = await _notification.SubscribeNewsletter(
                new SubscribeNewsletterRequest
                {
                    Email      = request.Email,
                    Lang       = request.Lang,
                    SourcePage = request.SourcePage,
                },
                forwardedFor: request.ClientIp);

            return resp?.Body ?? new NewsletterSubscribeResponse { Accepted = false };
        }
        catch (Refit.ApiException ex)
        {
            _logger.LogWarning(ex, "Newsletter subscribe failed (status {Status}).", ex.StatusCode);
            throw new AizenBusinessException("Could not subscribe right now. Please try again later.");
        }
    }
}
