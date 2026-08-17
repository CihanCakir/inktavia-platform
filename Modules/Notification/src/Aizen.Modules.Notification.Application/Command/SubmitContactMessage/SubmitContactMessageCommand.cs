using Aizen.Core.CQRS.Message;
using Aizen.Modules.Notification.Abstraction.Response;

namespace Aizen.Modules.Notification.Application.Command.SubmitContactMessage;

/// <summary>
/// M4 — submit an anonymous website contact message. Untrusted payload: validated, spam-scored, persisted, and (below
/// the threshold) fanned out to admins. Always returns <see cref="SubmitContactResponse"/> with <c>Accepted=true</c>
/// and a ticket ref — a spammer learns nothing.
/// </summary>
public sealed class SubmitContactMessageCommand : AizenCommand<SubmitContactResponse>
{
    public string  Name         { get; init; } = default!;
    public string  Email        { get; init; } = default!;
    public string  Subject      { get; init; } = default!;
    public string  Message      { get; init; } = default!;
    public string? SourcePage   { get; init; }
    public string? CaptchaToken { get; init; }
    public string? Honeypot     { get; init; }

    /// <summary>Submitter IP, forwarded by the BFF (X-Forwarded-For). The module HASHES it (salted) — never stored raw.</summary>
    public string? ClientIp     { get; init; }
}
