using System.Security.Cryptography;
using System.Text;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Abstraction.Response;
using Aizen.Modules.Notification.Application.Command.SubmitContactMessage;
using Aizen.Modules.Notification.Application.Services;
using Aizen.Modules.Notification.Domain.Entities;
using Aizen.Modules.Notification.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aizen.Modules.Notification.Application.Command.SubscribeNewsletter;

/// <summary>
/// P1 — idempotent, enumeration-safe newsletter sign-up. Reuses the CONTACT spam scorer + <see cref="ContactIntakeOptions"/>
/// (same policy — do not invent a new one): score ≥ threshold ⇒ the Pending row is still persisted but NO confirmation
/// mail is sent (a bot learns nothing), exactly as contact intake persists-but-skips-notify. Below the threshold a
/// single confirm mail is sent. The raw IP is never stored (salted hash only), and no raw address is ever logged.
/// </summary>
public sealed class SubscribeNewsletterCommandHandler
    : AizenCommandHandler<SubscribeNewsletterCommand, NewsletterSubscribeResponse>
{
    private readonly INewsletterSubscriberRepository _repository;
    private readonly INewsletterMailer _mailer;
    private readonly INewsletterLinkBuilder _links;
    private readonly ContactIntakeOptions _contactOptions;
    private readonly NewsletterIntakeOptions _options;
    private readonly ILogger<SubscribeNewsletterCommandHandler> _logger;

    public SubscribeNewsletterCommandHandler(
        INewsletterSubscriberRepository repository,
        INewsletterMailer mailer,
        INewsletterLinkBuilder links,
        IOptions<ContactIntakeOptions> contactOptions,
        IOptions<NewsletterIntakeOptions> options,
        ILogger<SubscribeNewsletterCommandHandler> logger)
    {
        _repository = repository;
        _mailer = mailer;
        _links = links;
        _contactOptions = contactOptions.Value;
        _options = options.Value;
        _logger = logger;
    }

    public override async Task<NewsletterSubscribeResponse?> Handle(
        SubscribeNewsletterCommand request, CancellationToken cancellationToken)
    {
        var emailNorm = NewsletterSubscriberEntity.Normalize(request.Email);
        var lang      = (request.Lang ?? "en").Trim().ToLowerInvariant();
        var ipHash    = HashIp(request.ClientIp);

        // Same scoring policy as contact intake: honeypot short-circuit (n/a here — no honeypot field) + a per-IP
        // submission-rate penalty. There is no message body, so the pure-text heuristics contribute their empty-body
        // baseline; the meaningful signal is the rate burst.
        var score = ContactSpamScorer.Score(subject: null, message: null, honeypot: null, _contactOptions);
        if (ipHash is not null && score < ContactSpamScorer.MaxScore)
        {
            var since = DateTimeOffset.UtcNow.AddMinutes(-_contactOptions.RateWindowMinutes);
            var recent = await _repository.CountRecentByIpHashAsync(ipHash, since, cancellationToken);
            if (recent >= _contactOptions.RateMaxPerWindow)
                score = Math.Min(score + 50, ContactSpamScorer.MaxScore);
        }

        var ttl = TimeSpan.FromHours(_options.ConfirmationTokenTtlHours);
        var existing = await _repository.GetByEmailAsync(emailNorm, cancellationToken);

        if (existing is null)
        {
            var entity = NewsletterSubscriberEntity.Create(emailNorm, lang, request.SourcePage, score, ipHash);
            entity.IssueConfirmationToken(ttl);
            await _repository.AddAsync(entity, cancellationToken);
            await MaybeSendConfirmationAsync(entity, score, cancellationToken);
            return Accepted();
        }

        switch (existing.Status)
        {
            case NewsletterSubscriberStatus.Confirmed:
            case NewsletterSubscriberStatus.Suppressed:
                // Enumeration protection (Confirmed) / sticky admin suppression (Suppressed): succeed, change nothing,
                // send nothing. The caller can never tell this address was already subscribed or suppressed.
                _logger.LogInformation(
                    "Newsletter subscribe for existing subscriber {Id} ({Status}); no-op, no mail.",
                    existing.Id, existing.Status);
                return Accepted();

            case NewsletterSubscriberStatus.Pending:
                existing.IssueConfirmationToken(ttl);   // re-issue on the SAME row (no duplicate)
                await _repository.UpdateAsync(existing, cancellationToken);
                await MaybeSendConfirmationAsync(existing, score, cancellationToken);
                return Accepted();

            case NewsletterSubscriberStatus.Unsubscribed:
            case NewsletterSubscriberStatus.Bounced:
                existing.Resubscribe();                 // back to Pending
                existing.IssueConfirmationToken(ttl);
                await _repository.UpdateAsync(existing, cancellationToken);
                await MaybeSendConfirmationAsync(existing, score, cancellationToken);
                return Accepted();

            default:
                return Accepted();
        }
    }

    private async Task MaybeSendConfirmationAsync(NewsletterSubscriberEntity entity, int score, CancellationToken ct)
    {
        if (score >= _contactOptions.SpamThreshold)
        {
            _logger.LogInformation(
                "Newsletter subscribe {Id} scored {Score} ≥ threshold {Threshold}; persisted, confirmation mail NOT sent.",
                entity.Id, score, _contactOptions.SpamThreshold);
            return;
        }

        var confirmUrl = _links.BuildConfirmUrl(entity.Lang, entity.ConfirmationToken!);
        var unsubUrl   = _links.BuildUnsubscribeUrl(entity.Lang, entity.UnsubscribeToken);

        try
        {
            await _mailer.SendConfirmationAsync(entity.Email, entity.Lang, confirmUrl, unsubUrl, ct);
            _logger.LogInformation("Newsletter confirmation mail sent for subscriber {Id}.", entity.Id);
        }
        catch (Exception ex)
        {
            // Best-effort: a mail hiccup must not fail the request (the Pending row is persisted; a re-subscribe re-sends).
            _logger.LogWarning(ex, "Newsletter confirmation mail failed for subscriber {Id}.", entity.Id);
        }
    }

    private static NewsletterSubscribeResponse Accepted() => new() { Accepted = true };

    // Salted SHA-256 of the IP → hex (same helper as contact intake). Never stores the raw IP; null when none forwarded.
    private string? HashIp(string? clientIp)
    {
        var ip = clientIp?.Trim();
        if (string.IsNullOrWhiteSpace(ip))
            return null;
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(_contactOptions.IpHashSalt + "|" + ip));
        return Convert.ToHexString(bytes);
    }
}
