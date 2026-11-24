using System.Security.Cryptography;
using System.Text;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Abstraction.Response;
using Aizen.Modules.Notification.Application.Services;
using Aizen.Modules.Notification.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Application.Command.ConfirmNewsletterSubscription;

/// <summary>
/// P1 — confirm the double opt-in. The token is single-use (cleared on <c>Confirm()</c>, so a second redemption of the
/// same token finds no row), expiry-checked, and constant-time compared. On success a welcome mail is sent (best-effort).
/// </summary>
public sealed class ConfirmNewsletterSubscriptionCommandHandler
    : AizenCommandHandler<ConfirmNewsletterSubscriptionCommand, NewsletterConfirmResponse>
{
    private readonly INewsletterSubscriberRepository _repository;
    private readonly INewsletterMailer _mailer;
    private readonly INewsletterLinkBuilder _links;
    private readonly ILogger<ConfirmNewsletterSubscriptionCommandHandler> _logger;

    public ConfirmNewsletterSubscriptionCommandHandler(
        INewsletterSubscriberRepository repository,
        INewsletterMailer mailer,
        INewsletterLinkBuilder links,
        ILogger<ConfirmNewsletterSubscriptionCommandHandler> logger)
    {
        _repository = repository;
        _mailer = mailer;
        _links = links;
        _logger = logger;
    }

    public override async Task<NewsletterConfirmResponse?> Handle(
        ConfirmNewsletterSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var token = request.Token?.Trim();
        if (string.IsNullOrEmpty(token))
            return Failed();

        var entity = await _repository.GetByConfirmationTokenAsync(token, cancellationToken);
        if (entity is null || entity.ConfirmationToken is null)
            return Failed();   // unknown or already-spent token (single-use)

        // Constant-time compare against the stored token (defence-in-depth over the indexed lookup).
        if (!FixedTimeEquals(entity.ConfirmationToken, token))
            return Failed();

        if (entity.ConfirmationTokenExpiresAt is null || entity.ConfirmationTokenExpiresAt < DateTimeOffset.UtcNow)
        {
            _logger.LogInformation("Newsletter confirm for subscriber {Id}: token expired.", entity.Id);
            return Failed();
        }

        if (entity.Status != NewsletterSubscriberStatus.Pending)
            return Failed();

        entity.Confirm();
        await _repository.UpdateAsync(entity, cancellationToken);

        try
        {
            var unsubUrl = _links.BuildUnsubscribeUrl(entity.Lang, entity.UnsubscribeToken);
            await _mailer.SendWelcomeAsync(entity.Email, entity.Lang, unsubUrl, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Newsletter welcome mail failed for subscriber {Id}.", entity.Id);
        }

        _logger.LogInformation("Newsletter subscriber {Id} confirmed.", entity.Id);
        return new NewsletterConfirmResponse { Confirmed = true };
    }

    private static bool FixedTimeEquals(string a, string b)
        => CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));

    private static NewsletterConfirmResponse Failed() => new() { Confirmed = false };
}
