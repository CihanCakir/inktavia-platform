using System.Security.Cryptography;
using Aizen.Core.Domain;
using Aizen.Modules.Notification.Abstraction.Enum;

namespace Aizen.Modules.Notification.Domain.Entities;

/// <summary>
/// P1 — a website newsletter subscriber. DDD/private-setter style modelled on <see cref="ContactMessageEntity"/>.
/// Double opt-in is mandatory: a row is only a real subscriber once <see cref="Status"/> is
/// <see cref="NewsletterSubscriberStatus.Confirmed"/> (the emailed <see cref="ConfirmationToken"/> was redeemed).
/// PII minimum: the address (lower-cased) + the language it was entered in. <see cref="IpHash"/> is a SALTED hash for
/// abuse correlation, NEVER the raw IP. Illegal transitions throw <see cref="InvalidOperationException"/> (the module's
/// transition-guard convention — see <c>NotificationTemplateContentEntity.UpdateContent</c>).
/// </summary>
public sealed class NewsletterSubscriberEntity : AizenEntity
{
    public string  Email      { get; private set; } = default!;
    public string  Lang       { get; private set; } = default!;
    public string? SourcePage { get; private set; }

    public NewsletterSubscriberStatus Status { get; private set; }

    /// <summary>Opaque, single-use confirmation handle. Null once confirmed (single-use enforced by clearing it).</summary>
    public string?         ConfirmationToken          { get; private set; }
    public DateTimeOffset? ConfirmationTokenExpiresAt { get; private set; }
    public DateTimeOffset? ConfirmedAt                { get; private set; }
    public DateTimeOffset? UnsubscribedAt             { get; private set; }

    /// <summary>Stable, never-expiring token behind the one-click unsubscribe link (issued once at creation).</summary>
    public string UnsubscribeToken { get; private set; } = default!;

    public int     SpamScore { get; private set; }
    /// <summary>Salted hash of the submitter IP (never the raw IP). Null when no IP was forwarded.</summary>
    public string? IpHash    { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private NewsletterSubscriberEntity() { }

    /// <summary>Creates a fresh <see cref="NewsletterSubscriberStatus.Pending"/> row. Email is normalised (trim +
    /// lower-case) by the caller BEFORE the uniqueness check; a stable unsubscribe token is minted here.</summary>
    public static NewsletterSubscriberEntity Create(
        string email, string lang, string? sourcePage, int spamScore, string? ipHash)
    {
        var now = DateTimeOffset.UtcNow;
        return new NewsletterSubscriberEntity
        {
            Email            = Normalize(email),
            Lang             = NormalizeLang(lang),
            SourcePage       = sourcePage,
            Status           = NewsletterSubscriberStatus.Pending,
            UnsubscribeToken = NewToken(),
            SpamScore        = spamScore,
            IpHash           = ipHash,
            CreatedAt        = now,
            UpdatedAt        = now,
        };
    }

    /// <summary>Test/seed factory with explicit status + timestamps (production code uses <see cref="Create"/> +
    /// the transition methods). Mirrors <c>NotificationEntity.CreateSeed</c>.</summary>
    public static NewsletterSubscriberEntity CreateSeed(
        string email, string lang, NewsletterSubscriberStatus status, DateTimeOffset createdAt,
        string? sourcePage = null, int spamScore = 0, DateTimeOffset? confirmedAt = null)
        => new()
        {
            Email            = Normalize(email),
            Lang             = NormalizeLang(lang),
            SourcePage       = sourcePage,
            Status           = status,
            UnsubscribeToken = NewToken(),
            SpamScore        = spamScore,
            ConfirmedAt      = confirmedAt,
            CreatedAt        = createdAt,
            UpdatedAt        = createdAt,
        };

    /// <summary>Canonical email form used for storage and the uniqueness check.</summary>
    public static string Normalize(string email) => (email ?? string.Empty).Trim().ToLowerInvariant();

    private static string NormalizeLang(string lang)
    {
        var l = (lang ?? string.Empty).Trim().ToLowerInvariant();
        return string.IsNullOrEmpty(l) ? "en" : l;
    }

    /// <summary>Mint (or re-issue) the confirmation token, valid for <paramref name="ttl"/>. Only a
    /// <see cref="NewsletterSubscriberStatus.Pending"/> row may hold a token (a confirmed row has none).</summary>
    public void IssueConfirmationToken(TimeSpan ttl)
    {
        if (Status != NewsletterSubscriberStatus.Pending)
            throw new InvalidOperationException(
                $"Only a Pending subscriber can be issued a confirmation token; subscriber {Id} is {Status}.");

        ConfirmationToken          = NewToken();
        ConfirmationTokenExpiresAt = DateTimeOffset.UtcNow.Add(ttl);
        UpdatedAt                  = DateTimeOffset.UtcNow;
    }

    /// <summary>Double opt-in complete: Pending → Confirmed, clears the (now spent) token, stamps <see cref="ConfirmedAt"/>.</summary>
    public void Confirm()
    {
        if (Status != NewsletterSubscriberStatus.Pending)
            throw new InvalidOperationException(
                $"Only a Pending subscriber can be confirmed; subscriber {Id} is {Status}.");

        Status                     = NewsletterSubscriberStatus.Confirmed;
        ConfirmationToken          = null;   // single-use: spent on confirm
        ConfirmationTokenExpiresAt = null;
        ConfirmedAt                = DateTimeOffset.UtcNow;
        UpdatedAt                  = DateTimeOffset.UtcNow;
    }

    /// <summary>One-click opt-out. Idempotent: a no-op when already Unsubscribed or (admin) Suppressed.</summary>
    public void Unsubscribe()
    {
        if (Status is NewsletterSubscriberStatus.Unsubscribed or NewsletterSubscriberStatus.Suppressed)
            return;   // idempotent — unsubscribing twice succeeds

        Status         = NewsletterSubscriberStatus.Unsubscribed;
        UnsubscribedAt = DateTimeOffset.UtcNow;
        UpdatedAt      = DateTimeOffset.UtcNow;
    }

    /// <summary>Bring an Unsubscribed/Bounced row back to Pending for a fresh double opt-in. An admin
    /// <see cref="NewsletterSubscriberStatus.Suppressed"/> row is sticky and refuses resubscribe.</summary>
    public void Resubscribe()
    {
        if (Status is not (NewsletterSubscriberStatus.Unsubscribed or NewsletterSubscriberStatus.Bounced))
            throw new InvalidOperationException(
                $"Only an Unsubscribed or Bounced subscriber can resubscribe; subscriber {Id} is {Status}.");

        Status         = NewsletterSubscriberStatus.Pending;
        ConfirmedAt    = null;
        UnsubscribedAt = null;
        UpdatedAt      = DateTimeOffset.UtcNow;
        // The caller issues a fresh confirmation token (IssueConfirmationToken) after resubscribing.
    }

    /// <summary>Mark the address as hard-bounced (confirmation/welcome mail undeliverable).</summary>
    public void MarkBounced()
    {
        if (Status == NewsletterSubscriberStatus.Suppressed)
            throw new InvalidOperationException(
                $"A Suppressed subscriber cannot be marked Bounced; subscriber {Id} is {Status}.");

        Status                     = NewsletterSubscriberStatus.Bounced;
        ConfirmationToken          = null;
        ConfirmationTokenExpiresAt = null;
        UpdatedAt                  = DateTimeOffset.UtcNow;
    }

    /// <summary>Admin manual suppression (abuse/complaint). Sticky: survives resubscribe.</summary>
    public void Suppress()
    {
        Status                     = NewsletterSubscriberStatus.Suppressed;
        ConfirmationToken          = null;
        ConfirmationTokenExpiresAt = null;
        UpdatedAt                  = DateTimeOffset.UtcNow;
    }

    // 256-bit URL-safe opaque token (Base64Url, no padding). Used for both the confirmation and unsubscribe handles.
    private static string NewToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}
