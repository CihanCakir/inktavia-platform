using Aizen.Bff.Marine.Web.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Notification.Abstraction.Request;
using Aizen.Modules.Notification.Abstraction.Response;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Web.Application.Newsletter.Command.ConfirmWebNewsletter;

public sealed class ConfirmWebNewsletterCommandHandler
    : AizenCommandHandler<ConfirmWebNewsletterCommand, NewsletterConfirmResponse>
{
    private readonly INotificationRemoteCall _notification;
    private readonly ILogger<ConfirmWebNewsletterCommandHandler> _logger;

    public ConfirmWebNewsletterCommandHandler(
        INotificationRemoteCall notification, ILogger<ConfirmWebNewsletterCommandHandler> logger)
    {
        _notification = notification;
        _logger = logger;
    }

    public override async Task<NewsletterConfirmResponse?> Handle(
        ConfirmWebNewsletterCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var resp = await _notification.ConfirmNewsletter(new ConfirmNewsletterRequest { Token = request.Token });
            return resp?.Body ?? new NewsletterConfirmResponse { Confirmed = false };
        }
        catch (Refit.ApiException ex)
        {
            _logger.LogWarning(ex, "Newsletter confirm failed (status {Status}).", ex.StatusCode);
            throw new AizenBusinessException("Could not confirm your subscription right now. Please try again later.");
        }
    }
}
