using System.Security.Cryptography;
using System.Text;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Abstraction.RemoteCall;
using Aizen.Modules.Notification.Abstraction.Response;
using Aizen.Modules.Notification.Application.Command.SendNotification;
using Aizen.Modules.Notification.Domain.Entities;
using Aizen.Modules.Notification.Domain.Interface.Repository;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aizen.Modules.Notification.Application.Command.SubmitContactMessage;

/// <summary>
/// M4 — validate (upstream) → score → persist → notify (below threshold) → always accept. The payload is untrusted:
/// nothing from the client is echoed back except the opaque ticket ref, the raw IP is never stored (salted hash only),
/// and a spam verdict never changes the response (a bot learns nothing).
/// </summary>
public sealed class SubmitContactMessageCommandHandler
    : AizenCommandHandler<SubmitContactMessageCommand, SubmitContactResponse>
{
    private readonly IContactMessageRepository _repository;
    private readonly INotificationIdentityRemoteCall _identity;
    private readonly ISender _sender;
    private readonly ContactIntakeOptions _options;
    private readonly ILogger<SubmitContactMessageCommandHandler> _logger;

    public SubmitContactMessageCommandHandler(
        IContactMessageRepository repository,
        INotificationIdentityRemoteCall identity,
        ISender sender,
        IOptions<ContactIntakeOptions> options,
        ILogger<SubmitContactMessageCommandHandler> logger)
    {
        _repository = repository;
        _identity = identity;
        _sender = sender;
        _options = options.Value;
        _logger = logger;
    }

    public override async Task<SubmitContactResponse?> Handle(
        SubmitContactMessageCommand request, CancellationToken cancellationToken)
    {
        // FUTURE: verify request.CaptchaToken against the configured provider (Turnstile/hCaptcha) and fold the
        // result into the score. Accepted-and-ignored until a provider is wired.

        var ipHash = HashIp(request.ClientIp);

        // Pure heuristics + a per-IP submission-rate penalty (needs the repository).
        var score = ContactSpamScorer.Score(request.Subject, request.Message, request.Honeypot, _options);
        if (ipHash is not null && score < ContactSpamScorer.MaxScore)
        {
            var since = DateTimeOffset.UtcNow.AddMinutes(-_options.RateWindowMinutes);
            var recent = await _repository.CountRecentByIpHashAsync(ipHash, since, cancellationToken);
            if (recent >= _options.RateMaxPerWindow)
                score = Math.Min(score + 50, ContactSpamScorer.MaxScore);
        }

        var ticketRef = GenerateTicketRef();

        var entity = ContactMessageEntity.Create(
            name: request.Name.Trim(),
            email: request.Email.Trim(),
            subject: request.Subject.Trim(),
            message: request.Message.Trim(),
            sourcePage: request.SourcePage,
            spamScore: score,
            ticketRef: ticketRef,
            ipHash: ipHash);
        await _repository.AddAsync(entity, cancellationToken);

        if (score < _options.SpamThreshold)
            await NotifyAdmins(request, ticketRef, cancellationToken);
        else
            _logger.LogInformation("Contact {TicketRef} scored {Score} ≥ threshold {Threshold}; persisted, admins not notified.",
                ticketRef, score, _options.SpamThreshold);

        // Always accept — a spammer must not be able to distinguish outcomes.
        return new SubmitContactResponse { Accepted = true, TicketRef = ticketRef };
    }

    private async Task NotifyAdmins(SubmitContactMessageCommand request, string ticketRef, CancellationToken ct)
    {
        List<long> adminIds;
        try
        {
            adminIds = (await _identity.GetAdminUserIds())?.Body ?? new List<long>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Contact {TicketRef}: could not resolve admin ids; not notified (ticket still persisted).", ticketRef);
            return;
        }

        foreach (var adminId in adminIds)
        {
            await _sender.Send(new SendNotificationCommand
            {
                RecipientUserId = adminId,
                Type = NotificationType.ContactReceived,
                Channel = NotificationChannel.InApp,
                Variables = new Dictionary<string, string>
                {
                    { "name", request.Name.Trim() },
                    { "subject", request.Subject.Trim() },
                    { "ticketRef", ticketRef },
                },
                MetadataJson = $"{{\"ticketRef\":\"{ticketRef}\"}}",
                ReferenceType = "ContactMessage",
            }, ct);
        }

        _logger.LogInformation("Contact {TicketRef} → notified {Count} admins.", ticketRef, adminIds.Count);
    }

    // Opaque public ref — random, not the DB id. e.g. CT-9F3A1C2B7E44.
    private static string GenerateTicketRef()
        => "CT-" + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();

    // Salted SHA-256 of the IP → hex. Never stores the raw IP; null when no IP was forwarded.
    private string? HashIp(string? clientIp)
    {
        var ip = clientIp?.Trim();
        if (string.IsNullOrWhiteSpace(ip))
            return null;
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(_options.IpHashSalt + "|" + ip));
        return Convert.ToHexString(bytes);
    }
}
