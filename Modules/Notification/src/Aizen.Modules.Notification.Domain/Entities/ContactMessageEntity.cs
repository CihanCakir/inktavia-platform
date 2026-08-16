using Aizen.Core.Domain;
using Aizen.Modules.Notification.Abstraction.Enum;

namespace Aizen.Modules.Notification.Domain.Entities;

/// <summary>
/// M4 — an inbound anonymous website contact ticket (folded into Notification as an interim owner; a future Support
/// module could take it over). PII minimum: name/email/message only — no cookies, no tracking ids. <see cref="IpHash"/>
/// is a SALTED hash for rate/abuse correlation, NEVER the raw IP. <see cref="TicketRef"/> is the opaque public handle
/// returned to the caller — never the database <see cref="AizenEntity.Id"/>.
/// </summary>
public sealed class ContactMessageEntity : AizenEntity
{
    public string  Name       { get; private set; } = default!;
    public string  Email      { get; private set; } = default!;
    public string  Subject    { get; private set; } = default!;
    public string  Message    { get; private set; } = default!;
    public string? SourcePage { get; private set; }

    public ContactMessageStatus Status   { get; private set; }
    public int                  SpamScore { get; private set; }

    /// <summary>Opaque public reference (e.g. <c>CT-XXXXXXXX</c>). The only id ever shown to the anonymous caller.</summary>
    public string  TicketRef { get; private set; } = default!;

    /// <summary>Salted hash of the submitter IP (never the raw IP). Null when no IP was forwarded.</summary>
    public string? IpHash    { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    private ContactMessageEntity() { }

    public static ContactMessageEntity Create(
        string name, string email, string subject, string message,
        string? sourcePage, int spamScore, string ticketRef, string? ipHash)
        => new()
        {
            Name       = name,
            Email      = email,
            Subject    = subject,
            Message    = message,
            SourcePage = sourcePage,
            Status     = ContactMessageStatus.New,
            SpamScore  = spamScore,
            TicketRef  = ticketRef,
            IpHash     = ipHash,
            CreatedAt  = DateTimeOffset.UtcNow,
        };
}
