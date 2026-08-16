using Aizen.Bff.Marine.Web.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Notification.Abstraction.Request;
using Aizen.Modules.Notification.Abstraction.Response;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Web.Application.Contact.Command.SubmitWebContact;

public sealed class SubmitWebContactCommandHandler
    : AizenCommandHandler<SubmitWebContactCommand, SubmitContactResponse>
{
    private readonly INotificationRemoteCall _notification;
    private readonly ILogger<SubmitWebContactCommandHandler> _logger;

    public SubmitWebContactCommandHandler(
        INotificationRemoteCall notification, ILogger<SubmitWebContactCommandHandler> logger)
    {
        _notification = notification;
        _logger = logger;
    }

    public override async Task<SubmitContactResponse?> Handle(
        SubmitWebContactCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Forward the untrusted payload; the module owns validation/spam/persistence/notify. The caller IP goes
            // in X-Forwarded-For (the module hashes it) — the browser never sends its own IP.
            var resp = await _notification.SubmitContact(
                new SubmitContactRequest
                {
                    Name         = request.Name,
                    Email        = request.Email,
                    Subject      = request.Subject,
                    Message      = request.Message,
                    SourcePage   = request.SourcePage,
                    CaptchaToken = request.CaptchaToken,
                    Honeypot     = request.Honeypot,
                },
                forwardedFor: request.ClientIp);

            return resp?.Body ?? new SubmitContactResponse { Accepted = false };
        }
        catch (Refit.ApiException ex)
        {
            _logger.LogWarning(ex, "Contact submit failed (status {Status}).", ex.StatusCode);
            throw new AizenBusinessException("Could not submit your message right now. Please try again later.");
        }
    }
}
