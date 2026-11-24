using Aizen.Core.CQRS.Message;
using Aizen.Modules.Notification.Abstraction.Response;

namespace Aizen.Modules.Notification.Application.Command.SubscribeNewsletter;

/// <summary>
/// P1 — subscribe an email to the newsletter (double opt-in). Idempotent and enumeration-safe: a new address creates a
/// Pending row + confirm mail; an existing Pending re-issues its token (no second row); an already-Confirmed address
/// returns success WITHOUT a second mail and WITHOUT revealing it is subscribed; an Unsubscribed address returns to
/// Pending with a fresh token. Never throws on a duplicate; never returns "already subscribed".
/// </summary>
public sealed class SubscribeNewsletterCommand : AizenCommand<NewsletterSubscribeResponse>
{
    public string  Email      { get; init; } = default!;
    public string  Lang       { get; init; } = default!;
    public string? SourcePage { get; init; }

    /// <summary>Submitter IP, forwarded by the BFF (X-Forwarded-For). The module HASHES it (salted) — never stored raw.
    /// Named <c>ClientIp</c> (not <c>IpHash</c>) because hashing lives here where the salt is, exactly as contact intake
    /// does — see the report note on this deliberate divergence from the prompt's field name.</summary>
    public string? ClientIp   { get; init; }
}
